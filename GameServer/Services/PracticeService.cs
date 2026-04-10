using System.Text.Json;
using GameServer.DTO;
using GameServer.Network;
using GameServer.Entities;
using GameServer.Repositories;
using GameServer.Runtime;
using GameServer.World;
using GameShared.Messages;
using GameShared.Models;
using Microsoft.Extensions.DependencyInjection;

namespace GameServer.Services;

public sealed class PracticeService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly WorldManager _worldManager;
    private readonly CharacterRuntimeService _runtimeService;

    public PracticeService(
        IServiceScopeFactory scopeFactory,
        WorldManager worldManager,
        CharacterRuntimeService runtimeService)
    {
        _scopeFactory = scopeFactory;
        _worldManager = worldManager;
        _runtimeService = runtimeService;
    }

    public bool IsPracticing(CharacterCurrentStateDto? state)
    {
        return state is not null && state.CurrentState == CharacterRuntimeStateCodes.Practicing;
    }

    public bool IsPracticing(PlayerSession? player)
    {
        if (player is null)
            return false;

        return player.RuntimeState.CaptureSnapshot().CurrentState.CurrentState == CharacterRuntimeStateCodes.Practicing;
    }

    public async Task<PlayerPracticeSessionEntity?> GetBlockingSessionAsync(
        Guid playerId,
        CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<PlayerPracticeSessionRepository>();
        return await repository.GetBlockingSessionAsync(playerId, cancellationToken);
    }

    public async Task<PlayerPracticeSessionEntity?> GetLatestSessionByTypeAsync(
        Guid playerId,
        PracticeType practiceType,
        CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<PlayerPracticeSessionRepository>();
        return await repository.GetLatestByTypeAsync(playerId, practiceType, cancellationToken);
    }

    public bool TryValidatePrivateHome(PlayerSession player, out MessageCode failureCode)
    {
        failureCode = MessageCode.None;
        if (!_worldManager.MapManager.TryGetInstance(player.MapId, player.InstanceId, out var instance) ||
            !instance.Definition.IsPrivatePerPlayer ||
            instance.Definition.Type != MapType.Home)
        {
            failureCode = MessageCode.PracticeRequiresPrivateHome;
            return false;
        }

        var currentState = player.RuntimeState.CaptureSnapshot().CurrentState;
        if (currentState.IsExpired || currentState.CurrentState == CharacterRuntimeStateCodes.LifespanExpired)
        {
            failureCode = MessageCode.CharacterActionsRestricted;
            return false;
        }

        return true;
    }

    public long CalculateAccumulatedActiveSeconds(PlayerPracticeSessionEntity session, DateTime utcNow)
    {
        var accumulated = Math.Max(0L, session.AccumulatedActiveSeconds);
        if (session.PracticeState != (int)PracticeSessionState.Active || !session.LastResumedAtUtc.HasValue)
            return accumulated;

        var elapsed = utcNow - session.LastResumedAtUtc.Value;
        if (elapsed <= TimeSpan.Zero)
            return accumulated;

        return accumulated + (long)Math.Floor(elapsed.TotalSeconds);
    }

    public long CalculateRemainingDurationSeconds(PlayerPracticeSessionEntity session, DateTime utcNow)
    {
        return Math.Max(0L, Math.Max(0L, session.TotalDurationSeconds) - CalculateAccumulatedActiveSeconds(session, utcNow));
    }

    public double CalculateProgress(PlayerPracticeSessionEntity session, DateTime utcNow)
    {
        if (session.TotalDurationSeconds <= 0L)
            return 1d;

        var accumulated = CalculateAccumulatedActiveSeconds(session, utcNow);
        return Math.Clamp((double)accumulated / session.TotalDurationSeconds, 0d, 1d);
    }

    public bool IsCancelLocked(PlayerPracticeSessionEntity session, DateTime utcNow)
    {
        return CalculateProgress(session, utcNow) >= Math.Clamp(session.CancelLockedProgress, 0d, 1d);
    }

    public PracticeSessionModel BuildSessionModel(PlayerPracticeSessionEntity session, DateTime utcNow)
    {
        return new PracticeSessionModel
        {
            PracticeSessionId = session.Id,
            PracticeType = session.PracticeType,
            PracticeState = session.PracticeState,
            DefinitionId = session.DefinitionId,
            Title = session.Title,
            TotalDurationSeconds = Math.Max(0L, session.TotalDurationSeconds),
            AccumulatedActiveSeconds = CalculateAccumulatedActiveSeconds(session, utcNow),
            RemainingDurationSeconds = CalculateRemainingDurationSeconds(session, utcNow),
            Progress = CalculateProgress(session, utcNow),
            CanCancel = session.PracticeState != (int)PracticeSessionState.ResultPendingAcknowledgement &&
                        session.PracticeState != (int)PracticeSessionState.Completed &&
                        session.PracticeState != (int)PracticeSessionState.Cancelled &&
                        !IsCancelLocked(session, utcNow),
            IsPaused = session.PracticeState == (int)PracticeSessionState.Paused,
            StartedUnixMs = ToUnixMs(session.StartedAtUtc),
            LastResumedUnixMs = ToUnixMs(session.LastResumedAtUtc),
            PausedUnixMs = ToUnixMs(session.PausedAtUtc),
            CompletedUnixMs = ToUnixMs(session.CompletedAtUtc)
        };
    }

    public PracticeSessionPayload? DeserializeRequestPayload(PlayerPracticeSessionEntity session)
    {
        return DeserializePayload<PracticeSessionPayload>(session.RequestPayloadJson);
    }

    public PracticeCompletionPayload? DeserializeResultPayload(PlayerPracticeSessionEntity session)
    {
        return DeserializePayload<PracticeCompletionPayload>(session.ResultPayloadJson);
    }

    public string SerializePayload<T>(T payload)
    {
        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    public async Task<CharacterSnapshotDto> AlignSnapshotStateAsync(
        CharacterSnapshotDto snapshot,
        CancellationToken cancellationToken = default)
    {
        if (snapshot.CurrentState is null)
            return snapshot;

        var blockingSession = await GetBlockingSessionAsync(snapshot.Character.CharacterId, cancellationToken);
        var nextStateCode = blockingSession?.PracticeState == (int)PracticeSessionState.Active
            ? CharacterRuntimeStateCodes.Practicing
            : (snapshot.CurrentState.CurrentState == CharacterRuntimeStateCodes.Practicing
                ? CharacterRuntimeStateCodes.Idle
                : snapshot.CurrentState.CurrentState);

        if (nextStateCode == snapshot.CurrentState.CurrentState)
            return snapshot;

        var updatedCurrentState = snapshot.CurrentState with
        {
            CurrentState = nextStateCode,
            LastSavedAt = DateTime.UtcNow
        };

        await using var scope = _scopeFactory.CreateAsyncScope();
        var characterService = scope.ServiceProvider.GetRequiredService<CharacterService>();
        var persistedCurrentState = await characterService.UpdateCharacterCurrentStateAsync(updatedCurrentState, cancellationToken);
        return snapshot with { CurrentState = persistedCurrentState };
    }

    public void SyncOnlinePlayerState(PlayerSession player, PlayerPracticeSessionEntity? blockingSession)
    {
        var currentState = player.RuntimeState.CaptureSnapshot().CurrentState;
        var shouldPractice = blockingSession?.PracticeState == (int)PracticeSessionState.Active;
        if (shouldPractice && currentState.CurrentState != CharacterRuntimeStateCodes.Practicing)
        {
            _runtimeService.ApplyCurrentStateMutation(
                player,
                state => state with { CurrentState = CharacterRuntimeStateCodes.Practicing });
            return;
        }

        if (!shouldPractice && currentState.CurrentState == CharacterRuntimeStateCodes.Practicing)
        {
            _runtimeService.ApplyCurrentStateMutation(
                player,
                state => state with { CurrentState = CharacterRuntimeStateCodes.Idle });
        }
    }

    public async Task<PracticeMutationResult> PauseAsync(
        ConnectionSession session,
        long? practiceSessionId,
        CancellationToken cancellationToken = default)
    {
        if (session.Player is null)
            return PracticeMutationResult.Failed(MessageCode.CharacterMustEnterWorld);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<PlayerPracticeSessionRepository>();
        var entity = await ResolveOwnedSessionAsync(repository, session.Player.CharacterData.CharacterId, practiceSessionId, cancellationToken);
        if (entity is null || entity.PracticeState != (int)PracticeSessionState.Active)
            return PracticeMutationResult.Failed(MessageCode.PracticeNotActive);

        var utcNow = DateTime.UtcNow;
        entity.AccumulatedActiveSeconds = CalculateAccumulatedActiveSeconds(entity, utcNow);
        entity.PracticeState = (int)PracticeSessionState.Paused;
        entity.LastResumedAtUtc = null;
        entity.PausedAtUtc = utcNow;
        entity.UpdatedAtUtc = utcNow;
        await repository.UpdateAsync(entity, cancellationToken);

        SyncOnlinePlayerState(session.Player, null);
        return PracticeMutationResult.Succeeded(entity);
    }

    public async Task<PracticeMutationResult> ResumeAsync(
        ConnectionSession session,
        long? practiceSessionId,
        CancellationToken cancellationToken = default)
    {
        if (session.Player is null)
            return PracticeMutationResult.Failed(MessageCode.CharacterMustEnterWorld);

        if (!TryValidatePrivateHome(session.Player, out var failureCode))
            return PracticeMutationResult.Failed(failureCode);

        var currentSnapshot = session.Player.RuntimeState.CaptureSnapshot();
        if (currentSnapshot.CurrentState.CurrentState == CharacterRuntimeStateCodes.Cultivating)
            return PracticeMutationResult.Failed(MessageCode.PracticeAlreadyActive);
        if (session.Player.IsStunned(DateTime.UtcNow))
            return PracticeMutationResult.Failed(MessageCode.CharacterCannotActWhileStunned);
        if (currentSnapshot.CurrentState.CurrentState == CharacterRuntimeStateCodes.Casting || session.Player.IsCastingSkill)
            return PracticeMutationResult.Failed(MessageCode.CharacterCannotActWhileCasting);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<PlayerPracticeSessionRepository>();
        var entity = await ResolveOwnedSessionAsync(repository, session.Player.CharacterData.CharacterId, practiceSessionId, cancellationToken);
        if (entity is null || entity.PracticeState != (int)PracticeSessionState.Paused)
            return PracticeMutationResult.Failed(MessageCode.PracticeNotActive);

        var blocking = await repository.GetBlockingSessionAsync(session.Player.CharacterData.CharacterId, cancellationToken);
        if (blocking is not null && blocking.Id != entity.Id)
            return PracticeMutationResult.Failed(MessageCode.PracticeAlreadyActive);

        var utcNow = DateTime.UtcNow;
        entity.PracticeState = (int)PracticeSessionState.Active;
        entity.LastResumedAtUtc = utcNow;
        entity.PausedAtUtc = null;
        entity.UpdatedAtUtc = utcNow;
        entity.CurrentMapId = session.Player.MapId;
        await repository.UpdateAsync(entity, cancellationToken);

        SyncOnlinePlayerState(session.Player, entity);
        return PracticeMutationResult.Succeeded(entity);
    }

    public async Task<PracticeMutationResult> CancelAsync(
        ConnectionSession session,
        long? practiceSessionId,
        CancellationToken cancellationToken = default)
    {
        if (session.Player is null)
            return PracticeMutationResult.Failed(MessageCode.CharacterMustEnterWorld);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<PlayerPracticeSessionRepository>();
        var entity = await ResolveOwnedSessionAsync(repository, session.Player.CharacterData.CharacterId, practiceSessionId, cancellationToken);
        if (entity is null ||
            (entity.PracticeState != (int)PracticeSessionState.Active && entity.PracticeState != (int)PracticeSessionState.Paused))
        {
            return PracticeMutationResult.Failed(MessageCode.PracticeNotActive);
        }

        if (IsCancelLocked(entity, DateTime.UtcNow))
            return PracticeMutationResult.Failed(MessageCode.PracticeCancelLocked);

        if (entity.PracticeState == (int)PracticeSessionState.Active)
            entity.AccumulatedActiveSeconds = CalculateAccumulatedActiveSeconds(entity, DateTime.UtcNow);

        entity.PracticeState = (int)PracticeSessionState.Cancelled;
        entity.LastResumedAtUtc = null;
        entity.PausedAtUtc = DateTime.UtcNow;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await repository.UpdateAsync(entity, cancellationToken);

        SyncOnlinePlayerState(session.Player, null);
        return PracticeMutationResult.Succeeded(entity);
    }

    public async Task<PracticeAcknowledgeResult> AcknowledgeResultAsync(
        ConnectionSession session,
        long? practiceSessionId,
        CancellationToken cancellationToken = default)
    {
        if (session.Player is null)
            return PracticeAcknowledgeResult.Failed(MessageCode.CharacterMustEnterWorld);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<PlayerPracticeSessionRepository>();
        var entity = await ResolveOwnedSessionAsync(repository, session.Player.CharacterData.CharacterId, practiceSessionId, cancellationToken);
        if (entity is null || entity.PracticeState != (int)PracticeSessionState.ResultPendingAcknowledgement)
            return PracticeAcknowledgeResult.Failed(MessageCode.PracticeNotActive);

        var utcNow = DateTime.UtcNow;
        entity.PracticeState = (int)PracticeSessionState.Completed;
        entity.ResultAcknowledgedAtUtc = utcNow;
        entity.UpdatedAtUtc = utcNow;
        await repository.UpdateAsync(entity, cancellationToken);

        SyncOnlinePlayerState(session.Player, null);
        return PracticeAcknowledgeResult.Succeeded(entity.Id);
    }

    private static TPayload? DeserializePayload<TPayload>(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
            return default;

        return JsonSerializer.Deserialize<TPayload>(payloadJson, JsonOptions);
    }

    private static long? ToUnixMs(DateTime? dateTime)
    {
        if (!dateTime.HasValue)
            return null;

        var utc = dateTime.Value.Kind == DateTimeKind.Utc
            ? dateTime.Value
            : DateTime.SpecifyKind(dateTime.Value, DateTimeKind.Utc);
        return new DateTimeOffset(utc).ToUnixTimeMilliseconds();
    }

    private static async Task<PlayerPracticeSessionEntity?> ResolveOwnedSessionAsync(
        PlayerPracticeSessionRepository repository,
        Guid playerId,
        long? practiceSessionId,
        CancellationToken cancellationToken)
    {
        if (practiceSessionId.HasValue)
        {
            var owned = await repository.GetByIdAsync(practiceSessionId.Value, cancellationToken);
            return owned is not null && owned.PlayerId == playerId ? owned : null;
        }

        return await repository.GetBlockingSessionAsync(playerId, cancellationToken);
    }
}

public readonly record struct PracticeMutationResult(
    bool Success,
    MessageCode Code,
    PlayerPracticeSessionEntity? Session)
{
    public static PracticeMutationResult Failed(MessageCode code) => new(false, code, null);

    public static PracticeMutationResult Succeeded(PlayerPracticeSessionEntity session) => new(true, MessageCode.None, session);
}

public readonly record struct PracticeAcknowledgeResult(
    bool Success,
    MessageCode Code,
    long? PracticeSessionId)
{
    public static PracticeAcknowledgeResult Failed(MessageCode code) => new(false, code, null);

    public static PracticeAcknowledgeResult Succeeded(long practiceSessionId) => new(true, MessageCode.None, practiceSessionId);
}
