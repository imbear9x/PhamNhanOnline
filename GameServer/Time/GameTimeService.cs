using GameServer.Entities;
using GameServer.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace GameServer.Time;

public sealed class GameTimeService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly object _stateGate = new();

    private GameTimeState _state;

    public GameTimeService(IServiceScopeFactory scopeFactory, GameTimeConfig bootstrapConfig)
    {
        _scopeFactory = scopeFactory;
        _state = LoadOrCreateState(bootstrapConfig);
    }

    public GameTimeConfig Config
    {
        get
        {
            lock (_stateGate)
            {
                return ToConfigSnapshot(_state);
            }
        }
    }

    public GameTimeState GetCurrentState()
    {
        lock (_stateGate)
        {
            return Clone(_state);
        }
    }

    public GameTimeSnapshot GetCurrentSnapshot()
    {
        GameTimeState state;
        lock (_stateGate)
        {
            state = Clone(_state);
        }

        return BuildSnapshot(state, DateTime.UtcNow);
    }

    public async Task<GameTimeState> UpdateTimeScaleAsync(
        double gameMinutesPerRealMinute,
        CancellationToken cancellationToken = default)
    {
        if (gameMinutesPerRealMinute <= 0)
            throw new ArgumentOutOfRangeException(nameof(gameMinutesPerRealMinute), "Game time scale must be positive.");

        var utcNow = DateTime.UtcNow;
        GameTimeState current;
        lock (_stateGate)
        {
            current = Clone(_state);
        }

        var currentSnapshot = BuildSnapshot(current, utcNow);
        var updated = Clone(current);
        updated.AnchorUtc = utcNow;
        updated.AnchorGameMinute = currentSnapshot.CurrentGameMinute;
        updated.GameMinutesPerRealMinute = gameMinutesPerRealMinute;
        updated.UpdatedAt = utcNow;

        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<GameTimeStateRepository>();
        await repository.UpdateAsync(updated, cancellationToken);

        lock (_stateGate)
        {
            _state = Clone(updated);
            return Clone(_state);
        }
    }

    private GameTimeState LoadOrCreateState(GameTimeConfig bootstrapConfig)
    {
        Validate(bootstrapConfig);

        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<GameTimeStateRepository>();
        var state = repository.GetPrimaryAsync().GetAwaiter().GetResult();

        if (state is not null)
        {
            Validate(state);
            return Normalize(state);
        }

        var utcNow = DateTime.UtcNow;
        var created = new GameTimeState
        {
            Id = GameTimeStateRepository.PrimaryId,
            AnchorUtc = NormalizeUtc(bootstrapConfig.AnchorUtc),
            AnchorGameMinute = bootstrapConfig.AnchorGameMinute,
            GameMinutesPerRealMinute = bootstrapConfig.GameMinutesPerRealMinute,
            DaysPerGameYear = bootstrapConfig.DaysPerGameYear,
            RuntimeSaveIntervalSeconds = bootstrapConfig.RuntimeSaveIntervalSeconds,
            DerivedStateRefreshIntervalSeconds = bootstrapConfig.DerivedStateRefreshIntervalSeconds,
            UpdatedAt = utcNow
        };

        repository.CreateAsync(created).GetAwaiter().GetResult();
        return Normalize(created);
    }

    private static GameTimeSnapshot BuildSnapshot(GameTimeState state, DateTime utcNow)
    {
        var normalizedAnchorUtc = NormalizeUtc(state.AnchorUtc);
        var elapsedRealMinutes = (utcNow - normalizedAnchorUtc).TotalMinutes;
        var deltaGameMinutes = (long)Math.Floor(elapsedRealMinutes * state.GameMinutesPerRealMinute);
        var currentGameMinute = checked(state.AnchorGameMinute + deltaGameMinutes);

        return new GameTimeSnapshot(
            utcNow,
            currentGameMinute,
            state.DaysPerGameYear,
            state.GameMinutesPerRealMinute);
    }

    private static GameTimeConfig ToConfigSnapshot(GameTimeState state) =>
        new()
        {
            AnchorUtc = NormalizeUtc(state.AnchorUtc),
            AnchorGameMinute = state.AnchorGameMinute,
            GameMinutesPerRealMinute = state.GameMinutesPerRealMinute,
            DaysPerGameYear = state.DaysPerGameYear,
            RuntimeSaveIntervalSeconds = state.RuntimeSaveIntervalSeconds,
            DerivedStateRefreshIntervalSeconds = state.DerivedStateRefreshIntervalSeconds
        };

    private static GameTimeState Clone(GameTimeState state) =>
        new()
        {
            Id = state.Id,
            AnchorUtc = state.AnchorUtc,
            AnchorGameMinute = state.AnchorGameMinute,
            GameMinutesPerRealMinute = state.GameMinutesPerRealMinute,
            DaysPerGameYear = state.DaysPerGameYear,
            RuntimeSaveIntervalSeconds = state.RuntimeSaveIntervalSeconds,
            DerivedStateRefreshIntervalSeconds = state.DerivedStateRefreshIntervalSeconds,
            UpdatedAt = state.UpdatedAt
        };

    private static GameTimeState Normalize(GameTimeState state)
    {
        var normalized = Clone(state);
        normalized.AnchorUtc = NormalizeUtc(normalized.AnchorUtc);
        normalized.UpdatedAt = NormalizeUtc(normalized.UpdatedAt);
        return normalized;
    }

    private static DateTime NormalizeUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

    private static void Validate(GameTimeConfig config)
    {
        if (config.GameMinutesPerRealMinute <= 0)
            throw new ArgumentOutOfRangeException(nameof(config.GameMinutesPerRealMinute), "Game time scale must be positive.");
        if (config.DaysPerGameYear <= 0)
            throw new ArgumentOutOfRangeException(nameof(config.DaysPerGameYear), "DaysPerGameYear must be positive.");
        if (config.RuntimeSaveIntervalSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(config.RuntimeSaveIntervalSeconds), "RuntimeSaveIntervalSeconds must be positive.");
        if (config.DerivedStateRefreshIntervalSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(config.DerivedStateRefreshIntervalSeconds), "DerivedStateRefreshIntervalSeconds must be positive.");
    }

    private static void Validate(GameTimeState state)
    {
        if (state.GameMinutesPerRealMinute <= 0)
            throw new ArgumentOutOfRangeException(nameof(state.GameMinutesPerRealMinute), "Game time scale must be positive.");
        if (state.DaysPerGameYear <= 0)
            throw new ArgumentOutOfRangeException(nameof(state.DaysPerGameYear), "DaysPerGameYear must be positive.");
        if (state.RuntimeSaveIntervalSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(state.RuntimeSaveIntervalSeconds), "RuntimeSaveIntervalSeconds must be positive.");
        if (state.DerivedStateRefreshIntervalSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(state.DerivedStateRefreshIntervalSeconds), "DerivedStateRefreshIntervalSeconds must be positive.");
    }
}
