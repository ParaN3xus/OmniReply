using OmniReply.MessageObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WatsonWebsocket;

namespace OmniReply.Core
{
    public class Channel
    {
        public class MessageOrigin
        {
            public required Channel Channel;
            public string? GroupId;
            public required string UserId;
        }

        public delegate void ChatMessageReceivedHandler(MessageOrigin sender, ReceivedChatMessage message);

        public static readonly List<Channel> channels = [];


        public Guid WsClientId { get; set; }
        public string Id {get; set;}

        private WsInterface wsInterface;
        private Responder msgResponder;

        public event ChatMessageReceivedHandler? ChatMessageReceived;

        public Channel(WsInterface wsInterface, Responder responder, Guid wsClientId, string id)
        {
            this.wsInterface = wsInterface;
            msgResponder = responder;
            WsClientId = wsClientId;
            Id = id;
            channels.Add(this);

            ChatMessageReceived += responder.OnChatMessage;
        }

        public void SendMessage(SendingMessage message)
        {
            if(Id == "console")
            {
                Console.WriteLine(message.Content);
                return;
            }

            wsInterface.SendMessage(WsClientId, message);
        }

        public void ReceivedChatMessage(MessageOrigin sender, ReceivedChatMessage message)
        {
            ChatMessageReceived?.Invoke(sender, message);
        }
    }
}
