using Main;
using ProiectAMCSD.Utils;
using System.Text.RegularExpressions;

namespace ProiectAMCSD
{
    public class Application : Algorithm
    {
        private MySystem _system;
		private string _host;
		private int _port;

        public Application(string host, int port)
        {
            _system = MySystem.GetInstance();
			_host = host;
			_port = port;
        }

        public override bool CanHandle(Message message)
        {
			return (message.Type == Message.Types.Type.PlDeliver && message.PlDeliver.Message.Type == Message.Types.Type.AppBroadcast)
				|| (message.Type == Message.Types.Type.PlDeliver && message.PlDeliver.Message.Type == Message.Types.Type.AppWrite)
				|| (message.Type == Message.Types.Type.PlDeliver && message.PlDeliver.Message.Type == Message.Types.Type.AppRead)
				|| (message.Type == Message.Types.Type.PlDeliver && message.PlDeliver.Message.Type == Message.Types.Type.AppPropose)
				|| message.Type == Message.Types.Type.BebDeliver
				|| message.Type == Message.Types.Type.NnarWriteReturn
				|| message.Type == Message.Types.Type.NnarReadReturn
				|| message.Type == Message.Types.Type.UcDecide
				|| message.Type == Message.Types.Type.ProcDestroySystem;
        }

        public override void DoHandle(Message message)
		{
			var msg = new Message { };

			switch (message.Type)
			{
				case Message.Types.Type.PlDeliver:
					{
						switch (message.PlDeliver.Message.Type)
						{
							case Message.Types.Type.AppBroadcast:
								{
									msg = new Message
									{
										Type = Message.Types.Type.BebBroadcast,
										ToAbstractionId = "app.beb",
										BebBroadcast = new BebBroadcast
										{
											Message = new Message
											{
												Type = Message.Types.Type.AppValue,
												ToAbstractionId = "app",
												AppValue = new AppValue
												{
													Value = message.PlDeliver.Message.AppBroadcast.Value
												}
											}
										}
									};
									break;
								}
							case Message.Types.Type.AppWrite:
								{
									msg = new Message
									{
										Type = Message.Types.Type.NnarWrite,
										FromAbstractionId = "app",
										ToAbstractionId = "app.nnar[" + message.PlDeliver.Message.AppWrite.Register + "]",
										NnarWrite = new NnarWrite
										{
											Value = message.PlDeliver.Message.AppWrite.Value
										}
									};
									break;
								}
							case Message.Types.Type.AppRead:
								{
									msg = new Message
									{
										Type = Message.Types.Type.NnarRead,
										FromAbstractionId = "app",
										ToAbstractionId = "app.nnar[" + message.PlDeliver.Message.AppRead.Register + "]",
										NnarRead = new NnarRead { }
									};
									break;
								}
							case Message.Types.Type.AppPropose:
								{
									SystemInfo.TOPIC = message.PlDeliver.Message.AppPropose.Topic;
									msg = new Message
									{
										Type = Message.Types.Type.UcPropose,
										FromAbstractionId = "app",
										ToAbstractionId = "app.uc[" + message.PlDeliver.Message.AppPropose.Topic + "]",
										UcPropose = new UcPropose
										{
											Value = message.PlDeliver.Message.AppPropose.Value
										}
									};
									MySystem.GetInstance().RegisterConsensus();
									break;
								}
						}
						if (msg != null)
						{
							_system.AddMessageToQueue(msg);
						}
						break;
					}
				case Message.Types.Type.BebDeliver:
					{
						msg = new Message
						{
							SystemId = "sys-1",
							Type = Message.Types.Type.NetworkMessage,
							NetworkMessage = new NetworkMessage
							{
								SenderHost = _host,
								SenderListeningPort = _port,
								Message = message.BebDeliver.Message
							}
						};
						MySystem.GetInstance().SendMessage(msg, SystemInfo.HUB_HOST, SystemInfo.HUB_PORT);
						break;
					}
				case Message.Types.Type.NnarWriteReturn:
					{ 
						msg = new Message
						{
							SystemId = "sys-1",
							Type = Message.Types.Type.NetworkMessage,
							NetworkMessage = new NetworkMessage
							{
								SenderHost = _host,
								SenderListeningPort = _port,
								Message = new Message
								{
									Type = Message.Types.Type.AppWriteReturn,
									ToAbstractionId = "app",
									AppWriteReturn = new AppWriteReturn
									{
										Register = GetRegisterId(message.FromAbstractionId.Split('.').ToList().Last()),
									}
								}
							}
						};
						MySystem.GetInstance().SendMessage(msg, SystemInfo.HUB_HOST, SystemInfo.HUB_PORT);
						break;
					}
				case Message.Types.Type.NnarReadReturn:
					{
						msg = new Message
						{
							SystemId = "sys-1",
							Type = Message.Types.Type.NetworkMessage,
							NetworkMessage = new NetworkMessage
							{
								SenderHost = _host,
								SenderListeningPort =_port,
								Message = new Message
								{
									Type = Message.Types.Type.AppReadReturn,
									ToAbstractionId = "app",
									AppReadReturn = new AppReadReturn
									{
										Register = GetRegisterId(message.FromAbstractionId.Split('.').ToList().Last()),
										Value = message.NnarReadReturn.Value
									}
								}
							}
						};
						MySystem.GetInstance().SendMessage(msg, SystemInfo.HUB_HOST, SystemInfo.HUB_PORT);
						break;
					}
				case Message.Types.Type.UcDecide:
					{
						msg = new Message
						{
							Type = Message.Types.Type.PlSend,
							FromAbstractionId = "app",
							ToAbstractionId = "app.pl",
							PlSend = new PlSend
							{
								Destination = new ProcessId
                                {
									Host = SystemInfo.HUB_HOST,
									Port = SystemInfo.HUB_PORT,
                                },
								Message = new Message
								{
									Type = Message.Types.Type.AppDecide,
									ToAbstractionId = "app",
									AppDecide = new AppDecide
									{
										Value = message.UcDecide.Value,
									},
								},
							}
						};
						if (msg != null)
						{
							_system.AddMessageToQueue(msg);
						}
						break;
					}
				case Message.Types.Type.ProcDestroySystem:
                    {
						Console.WriteLine("System Destroyed, exiting...");
						Environment.Exit(0);
						break;
					}
			}
		}

        public string GetRegisterId(string abstractionId)
		{
			var pattern = @"([^\[]*)(\[([^\]]*)\])?";
			var match = Regex.Match(abstractionId, pattern);
			return match.Groups[3].Value;
		}
	}
}
