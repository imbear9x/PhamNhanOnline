using GameServer.World;

namespace GameServer.Runtime;

public sealed class GameLoop
{
    private readonly WorldManager _worldManager;

    private readonly CancellationTokenSource _cts = new();
    private Thread? _thread;

    public GameLoop(WorldManager worldManager)
    {
        _worldManager = worldManager;
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

    }
}

