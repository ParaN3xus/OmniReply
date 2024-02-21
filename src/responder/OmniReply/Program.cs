using System.Text;
using WatsonWebsocket;
using Newtonsoft.Json;
using OmniReply.Core;
using OmniReply.MessageObjects;

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

            while (true)
            {
                var msg = Console.ReadLine();

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
        }
    }
}
