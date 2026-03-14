using System.Runtime.CompilerServices;
using System.Threading.Channels;
using GameServer.World;
using GameShared.Packets;
using LiteNetLib;

namespace GameServer.Network;

public sealed class ConnectionSession
{
    private readonly Channel<InboundPacketEnvelope> _inboundPackets = Channel.CreateUnbounded<InboundPacketEnvelope>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
    private readonly CancellationTokenSource _inboundProcessingCts = new();
    private int _pendingInboundPacketCount;

    public NetPeer Peer { get; }
    public int ConnectionId => Peer.Id;

    public Guid PlayerId { get; set; }
    public Guid SelectedCharacterId { get; set; }
    public string? ResumeToken { get; set; }
    public PlayerSession? Player { get; set; }
    public bool IsAuthenticated { get; set; }
    public bool AreCharacterActionsRestricted { get; set; }
    public Task? InboundProcessorTask { get; set; }
    public int PendingInboundPacketCount => Volatile.Read(ref _pendingInboundPacketCount);

    public ConnectionSession(NetPeer peer)
    {
        Peer = peer;
        PlayerId = Guid.Empty;
        SelectedCharacterId = Guid.Empty;
        AreCharacterActionsRestricted = false;
    }

    internal bool TryEnqueueInboundPacket(InboundPacketEnvelope envelope)
    {
        if (!_inboundPackets.Writer.TryWrite(envelope))
            return false;

        Interlocked.Increment(ref _pendingInboundPacketCount);
        return true;
    }

    internal async IAsyncEnumerable<InboundPacketEnvelope> ReadInboundPacketsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            _inboundProcessingCts.Token,
            cancellationToken);

        await foreach (var envelope in _inboundPackets.Reader.ReadAllAsync(linkedCts.Token))
        {
            Interlocked.Decrement(ref _pendingInboundPacketCount);
            yield return envelope;
        }
    }

    internal void StopInboundProcessing()
    {
        _inboundProcessingCts.Cancel();
        _inboundPackets.Writer.TryComplete();
    }
}

internal sealed record InboundPacketEnvelope(
    IPacket Packet,
    byte[] RawPayload,
    byte ChannelNumber,
    DeliveryMethod DeliveryMethod);
