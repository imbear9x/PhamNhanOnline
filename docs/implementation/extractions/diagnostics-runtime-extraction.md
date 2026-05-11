# title
Diagnostics runtime extraction

# scope
In-process server metrics collection for packets, queues, world ticks, and maintenance ticks.

# source files
- `GameServer/Diagnostics/ServerMetricsService.cs`
- `GameServer/Diagnostics/ServerMetricsLoggerService.cs`
- `GameServer/Network/NetworkServer.cs`
- `GameServer/Runtime/GameLoop.cs`
- `GameServer/Runtime/RuntimeMaintenanceService.cs`

# current runtime behavior
- `ServerMetricsService` tracks inbound/outbound packet counts and bytes, queue depths per active connection, processing durations, and exception counts using concurrent dictionaries plus atomic counters (`GameServer/Diagnostics/ServerMetricsService.cs`).
- Packet metrics are recorded separately for inbound enqueue/drop/process and outbound send paths (`GameServer/Diagnostics/ServerMetricsService.cs`).
- World tick metrics track total ticks, overruns, average/max tick duration, and last observed world-instance count (`GameServer/Diagnostics/ServerMetricsService.cs`).
- Maintenance tick metrics separately track save/refresh runs, overruns, and timing (`GameServer/Diagnostics/ServerMetricsService.cs`).
- `CaptureSnapshot(...)` produces a summarized immutable snapshot including top packet types by bytes/count and current online-player count (`GameServer/Diagnostics/ServerMetricsService.cs`).
- `ServerMetricsLoggerService` reads snapshots and logs them periodically for operational visibility (`GameServer/Diagnostics/ServerMetricsLoggerService.cs`).

# validations / guards
- Max-value tracking uses compare-exchange loops to avoid losing higher observed values under concurrency (`GameServer/Diagnostics/ServerMetricsService.cs`).
- Average calculations guard against divide-by-zero by returning zero when count is absent (`GameServer/Diagnostics/ServerMetricsService.cs`).
- Session removal clears queue-depth tracking when a connection exits (`GameServer/Diagnostics/ServerMetricsService.cs`).

# config/data dependencies
- Depends on live runtime/network callers to feed metrics; no external DB persistence is visible in this service.
- Online-player count is supplied at snapshot time by the caller.

# client/server touch points
- No gameplay packet surface; this is server observability only.
- Output appears through logs rather than client-facing game responses.

# edge cases
- Top packet-type views are truncated to the top five entries.
- Dropped inbound packets are recorded under a synthetic `PacketType#Dropped` key instead of a separate structure.

# unclear or suspicious behavior
- This looks operational rather than player-facing second-brain knowledge; whether it belongs in canonical gameplay docs is a scope decision.
- No persistence/export sink is visible beyond logger consumption.

# suggested canonical target docs
- `docs/ops/server-runtime-metrics.md`
