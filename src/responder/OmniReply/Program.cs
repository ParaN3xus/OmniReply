using System.Text;
using WatsonWebsocket;
using Newtonsoft.Json;
using OmniReply.Core;
using OmniReply.MessageObjects;
using System.Net;

namespace OmniReply
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            var responder = new Responder();
            var wsInterface = new WsInterface(responder);

            var consoleChannel = new Channel(wsInterface, responder, Guid.Empty, "console");

            var isConsoleIn = Console.In.Peek() != -1;
            while (isConsoleIn)
            {
                string? msg;

                try
                {
                    msg = Console.ReadLine();
                    if(msg == null)
                    {
                        break;
                    }
                }
                catch
                {
                    break;
                }

                consoleChannel.ReceivedChatMessage(new Channel.MessageOrigin
                {
                    Channel = consoleChannel,
                    UserId = "console"
                }, new ReceivedChatMessage
                {
                    Sender = new MessageTarget
                    {
                        UserId = "console"
                    },
                    Content = new MessageContent
                    {
                        Parts = new List<MessagePart>
                        {
                            MessagePart.FromString(msg)
                        }
                    }
                });
            }

            var sleepSpan = TimeSpan.FromSeconds(60);

            while (true)
            {
                Thread.Sleep(sleepSpan);
            }
        }
    }
}
