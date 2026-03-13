using GameServer.World;
using GameServer.Time;

namespace GameServer.Runtime;

public sealed class GameLoop
{
    private readonly WorldManager _worldManager;
    private readonly CharacterRuntimeService _runtimeService;
    private readonly CharacterRuntimeSaveService _runtimeSaveService;
    private readonly GameTimeService _gameTimeService;

    private readonly CancellationTokenSource _cts = new();
    private Thread? _thread;
    private DateTime _nextRuntimeSaveUtc = DateTime.UtcNow;
    private DateTime _nextDerivedStateRefreshUtc = DateTime.UtcNow;

    public GameLoop(
        WorldManager worldManager,
        CharacterRuntimeService runtimeService,
        CharacterRuntimeSaveService runtimeSaveService,
        GameTimeService gameTimeService)
    {
        _worldManager = worldManager;
        _runtimeService = runtimeService;
        _runtimeSaveService = runtimeSaveService;
        _gameTimeService = gameTimeService;
    }

    public void Start()
    {
        if (_thread is not null)
            return;

        _thread = new Thread(Run)
        {
            IsBackground = true,
            Name = "GameServer.GameLoop"
        };
        _thread.Start();
    }

    public void Stop()
    {
        _cts.Cancel();
        _thread?.Join(TimeSpan.FromSeconds(2));
        _thread = null;
    }

    private void Run()
    {
        var token = _cts.Token;

        while (!token.IsCancellationRequested)
        {
            UpdateWorld();
            Thread.Sleep(50); // 20 ticks per second
        }
    }

    private void UpdateWorld()
    {
        var instances = _worldManager.MapManager.GetAllInstancesSnapshot();

        foreach (var instance in instances)
        {
            instance.Update();
        }

        if (DateTime.UtcNow >= _nextRuntimeSaveUtc)
        {
            _runtimeSaveService.SaveDirtyPlayersAsync(_cts.Token).GetAwaiter().GetResult();
            _nextRuntimeSaveUtc = DateTime.UtcNow.AddSeconds(_gameTimeService.Config.RuntimeSaveIntervalSeconds);
        }

        if (DateTime.UtcNow >= _nextDerivedStateRefreshUtc)
        {
            _runtimeService.RefreshTimeDerivedStateForOnlinePlayersAsync(_cts.Token).GetAwaiter().GetResult();
            _nextDerivedStateRefreshUtc = DateTime.UtcNow.AddSeconds(_gameTimeService.Config.DerivedStateRefreshIntervalSeconds);
        }
    }
}
