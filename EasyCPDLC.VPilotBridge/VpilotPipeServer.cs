using EasyCPDLC.VPilotBridge.Protocol;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EasyCPDLC.VPilotBridge
{
    internal sealed class VpilotPipeServer : IDisposable
    {
        private const int QueueCapacity = 256;
        private readonly Action<VpilotBridgePacket> commandHandler;
        private readonly ConcurrentQueue<string> pending = new ConcurrentQueue<string>();
        private readonly object writerLock = new object();
        private readonly SemaphoreSlim pendingSignal = new SemaphoreSlim(0);
        private CancellationTokenSource cancellation;
        private NamedPipeServerStream activePipe;
        private Task worker;

        public VpilotPipeServer(Action<VpilotBridgePacket> commandHandler)
        {
            this.commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
        }

        public void Start()
        {
            if (cancellation != null)
            {
                return;
            }

            cancellation = new CancellationTokenSource();
            worker = Task.Run(() => ServerLoopAsync(cancellation.Token));
        }

        public void Publish(VpilotBridgePacket packet)
        {
            string line = VpilotBridgeProtocol.Encode(packet);
            pending.Enqueue(line);
            while (pending.Count > QueueCapacity && pending.TryDequeue(out _))
            {
            }
            pendingSignal.Release();
        }

        private async Task ServerLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                NamedPipeServerStream pipe = null;
                try
                {
                    pipe = CreateCurrentUserPipe();
                    lock (writerLock)
                    {
                        activePipe = pipe;
                    }
                    await pipe.WaitForConnectionAsync(token).ConfigureAwait(false);

                    using (StreamReader reader = new StreamReader(pipe, new UTF8Encoding(false), false, 4096, true))
                    using (StreamWriter writer = new StreamWriter(pipe, new UTF8Encoding(false), 4096, true))
                    using (CancellationTokenSource connectionCancellation =
                        CancellationTokenSource.CreateLinkedTokenSource(token))
                    {
                        Task writerTask = WriteLoopAsync(writer, connectionCancellation.Token);
                        try
                        {
                            while (!token.IsCancellationRequested && pipe.IsConnected)
                            {
                                string line = await reader.ReadLineAsync().ConfigureAwait(false);
                                if (line == null)
                                {
                                    break;
                                }

                                if (VpilotBridgeProtocol.TryDecode(line, out VpilotBridgePacket packet))
                                {
                                    commandHandler(packet);
                                }
                            }
                        }
                        finally
                        {
                            connectionCancellation.Cancel();
                            try { pipe.Dispose(); } catch { }
                            try { await writerTask.ConfigureAwait(false); } catch (OperationCanceledException) { }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }
                }
                catch
                {
                    // EasyCPDLC can restart independently; recreate the pipe.
                }
                finally
                {
                    lock (writerLock)
                    {
                        activePipe = null;
                    }
                    try { pipe?.Dispose(); } catch { }
                }
            }
        }

        private static NamedPipeServerStream CreateCurrentUserPipe()
        {
            SecurityIdentifier user = WindowsIdentity.GetCurrent().User;
            PipeSecurity security = new PipeSecurity();
            security.SetOwner(user);
            security.SetAccessRuleProtection(true, false);
            security.AddAccessRule(new PipeAccessRule(user, PipeAccessRights.FullControl, AccessControlType.Allow));

            return new NamedPipeServerStream(
                VpilotBridgeProtocol.PipeName,
                PipeDirection.InOut,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous,
                4096,
                4096,
                security);
        }

        private async Task WriteLoopAsync(StreamWriter writer, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await pendingSignal.WaitAsync(token).ConfigureAwait(false);

                while (pending.TryDequeue(out string line))
                {
                    try
                    {
                        await writer.WriteLineAsync(line).ConfigureAwait(false);
                        await writer.FlushAsync().ConfigureAwait(false);
                    }
                    catch
                    {
                        pending.Enqueue(line);
                        pendingSignal.Release();
                        throw;
                    }
                }
            }
        }

        public void Dispose()
        {
            CancellationTokenSource cancellationToDispose = cancellation;
            Task workerToWait = worker;
            NamedPipeServerStream pipeToDispose;

            cancellationToDispose?.Cancel();
            lock (writerLock)
            {
                pipeToDispose = activePipe;
                activePipe = null;
            }

            // Do not dispose while holding writerLock: ServerLoopAsync needs
            // the same lock during teardown.
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
