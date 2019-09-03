using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Blish_HUD.ArcDps;

namespace Blish_HUD.GameServices.ArcDps
{
    class PipeListener : IArcDpsListener
    {
        private readonly string _pipeName;
        private const string DEFAULT_PIPE_NAME = "arcdpspipe";
        private static readonly Logger Logger = Logger.GetLogger(typeof(PipeListener));
        private const int MESSAGE_HEADER_SIZE = 8;
        public event SocketListener.Message ReceivedMessage;

        private Task serverTask;
        private CancellationTokenSource source = new CancellationTokenSource();

        public PipeListener(string pipeName = DEFAULT_PIPE_NAME)
        {
            _pipeName = pipeName;
        }

        public void Start()
        {
            serverTask = Listen();
        }

        public void Stop()
        {
            source.Cancel();
        }

        private async Task Listen()
        {
            await Task.Run(() =>
            {
                while (true)
                {
                    using (NamedPipeServerStream pipeServer =
                        new NamedPipeServerStream(_pipeName, PipeDirection.In, 1, PipeTransmissionMode.Message))
                    {
                        try
                        {
                            Logger.Debug("Waiting for client connection...");
                            pipeServer.WaitForConnection();

                            Logger.Debug("Client connected.");

                            while (pipeServer.IsConnected)
                            {
                                var header = new byte[MESSAGE_HEADER_SIZE];
                                pipeServer.Read(header, 0, MESSAGE_HEADER_SIZE);
                                var length = header[0];

                                if (length is 0)
                                {
                                    Logger.Debug("Length of message was 0.");
                                    continue;
                                }

                                var array = new byte[length];
                                pipeServer.Read(array, 0, length);
                                var combatEvent = CombatParser.ProcessCombat(array);
                                var src = combatEvent.Src;
                                var dest = combatEvent.Dst;
                                var ev = combatEvent.Ev;
                                if (src.Self == 1 && !combatEvent.Ev.Buff && dest.Id != 0 && ev.Iff == 1)
                                {
                                    Logger.Debug($"{combatEvent.Id} {combatEvent.Src.Name} hit {dest.Name} for {ev.Value} with {combatEvent.SkillName}");
                                }
                                //Task.Run(() =>
                                //{
                                //    var message = array;
                                //    ProcessMessage(array);
                                //});
                            }
                        }
                        catch (IOException e)
                        {
                            Logger.Debug("ERROR: {0}", e.Message);
                        }
                    }
                }
            }, source.Token);
        }

        private void ProcessMessage(byte[] messageData)
        {
            ReceivedMessage?.Invoke(new MessageData {Message = messageData});
        }
    }
}