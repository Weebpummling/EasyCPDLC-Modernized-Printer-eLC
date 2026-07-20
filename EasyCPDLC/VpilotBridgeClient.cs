using EasyCPDLC.VPilotBridge.Protocol;
using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace EasyCPDLC
{
    internal sealed class VpilotBridgeClient : IDisposable
    {
        private readonly object writerLock = new();
        private readonly Channel<string> outbound = Channel.CreateBounded<string>(new BoundedChannelOptions(64)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false
        });
        private CancellationTokenSource cancellation;
        private NamedPipeClientStream activePipe;
        private Task worker;

        public event EventHandler<VpilotBridgePacket> PacketReceived;
        public event EventHandler<bool> ConnectionChanged;

        public bool IsConnected { get; private set; }

        public void Start()
        {
            if (worker != null)
            {
                return;
            }

            cancellation = new CancellationTokenSource();
            worker = Task.Run(() => ConnectionLoopAsync(cancellation.Token));
        }

        public bool TrySendPrivateMessage(string recipient, string message)
        {
            VpilotBridgePacket packet = new()
            {
                Kind = VpilotBridgePacketKind.SendPrivateMessage,
                Id = Guid.NewGuid().ToString("N"),
                TimestampUtc = DateTime.UtcNow,
                Peer = (recipient ?? string.Empty).Trim(),
                Message = message ?? string.Empty
            };
            lock (writerLock)
            {
                return IsConnected && outbound.Writer.TryWrite(VpilotBridgeProtocol.Encode(packet));
            }
        }

        private async Task ConnectionLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    using NamedPipeClientStream pipe = new(
                        ".",
                        VpilotBridgeProtocol.PipeName,
                        PipeDirection.InOut,
                        PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                    await pipe.ConnectAsync(2000, token).ConfigureAwait(false);

                    using StreamReader reader = new(pipe, new UTF8Encoding(false), false, 4096, true);
                    using StreamWriter writer = new(pipe, new UTF8Encoding(false), 4096, true) { AutoFlush = true };
                    lock (writerLock)
                    {
                        activePipe = pipe;
                    }
                    SetConnected(true);

                    using CancellationTokenSource connectionCancellation =
                        CancellationTokenSource.CreateLinkedTokenSource(token);
                    Task writerTask = WriteLoopAsync(writer, connectionCancellation.Token);
                    try
                    {
                        while (!token.IsCancellationRequested && pipe.IsConnected)
                        {
                            string line = await reader.ReadLineAsync(token).ConfigureAwait(false);
                            if (line == null)
                            {
                                break;
                            }

                            if (VpilotBridgeProtocol.TryDecode(line, out VpilotBridgePacket packet))
                            {
                                PacketReceived?.Invoke(this, packet);
                            }
                        }
                    }
                    finally
                    {
                        connectionCancellation.Cancel();
                        try { await writerTask.ConfigureAwait(false); } catch (OperationCanceledException) { }
                        while (outbound.Reader.TryRead(out _)) { }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception)
                {
                    // vPilot may not be installed or running. Retry quietly.
                }
                finally
                {
                    lock (writerLock)
                    {
                        activePipe = null;
                    }
                    SetConnected(false);
                }

                try
                {
                    await Task.Delay(2000, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private async Task WriteLoopAsync(StreamWriter writer, CancellationToken token)
        {
            while (await outbound.Reader.WaitToReadAsync(token).ConfigureAwait(false))
            {
                while (outbound.Reader.TryRead(out string line))
                {
                    await writer.WriteLineAsync(line.AsMemory(), token).ConfigureAwait(false);
                }
            }
        }

        private void SetConnected(bool connected)
        {
            if (IsConnected == connected)
            {
                return;
            }

            IsConnected = connected;
            ConnectionChanged?.Invoke(this, connected);
        }

        public void Dispose()
        {
            CancellationTokenSource cancellationToDispose = cancellation;
            Task workerToWait = worker;
            NamedPipeClientStream pipeToDispose;

            cancellationToDispose?.Cancel();
            lock (writerLock)
            {
                pipeToDispose = activePipe;
                activePipe = null;
            }

            // Never dispose the pipe while holding writerLock. The connection
            // loop takes the same lock in its finally block, so doing both can
            // deadlock application shutdown while an asynchronous read ends.
            try { pipeToDispose?.Dispose(); } catch { }
            if (workerToWait != null && Task.CurrentId != workerToWait.Id)
            {
                try { workerToWait.Wait(TimeSpan.FromSeconds(2)); } catch { }
            }

            cancellationToDispose?.Dispose();
            cancellation = null;
            worker = null;
        }
    }
}
