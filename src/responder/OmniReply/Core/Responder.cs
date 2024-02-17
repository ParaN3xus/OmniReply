using OmniReply.MessageObjects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static OmniReply.Core.Channel;

namespace OmniReply.Core
{
    public class Responder
    {
        public Responder()
        {
            //wsInterface.MessageReceived += WsInterface_MessageReceived;
            Plugin.InitPlugins();
        }


        public void OnChatMessage(MessageOrigin sender, ReceivedChatMessage message)
        {
            var messageParts = message.Content.Parts;

            var msgText = MessageContent.ConvertToString(messageParts);

            var sessionId = sender.UserId;
            var isGroup = false;

            if(sender.GroupId != null)
            {
                sessionId = sender.GroupId;
                isGroup = true;
            }

            var session = Session.sessions.FirstOrDefault(s => s.SessionId == sessionId && s.isGroup == isGroup);


            object? result = null;
            try
            {
                if (session == null)
                {
                    session = new Session(sessionId, isGroup);
                }

                if(msgText.StartsWith('$'))
                {
                    result = session.RunCode(msgText);
                }
                else if(msgText.StartsWith('#'))
                {
                    result = session.RunCommand(msgText, sender);
                }
                else
                {
                    result = session.RunPluginCommand(message);
                }
            }
            catch (Exception e)
            {
                result = e.Message;
            }

            if (result == null)
            {
                return;
            }

            if (result == string.Empty && msgText.StartsWith('#'))
            {
                return;
            }

            List<MessagePart> responseParts;

            if(result is List<MessagePart>)
            {
                responseParts = (List<MessagePart>)result;
            }
            else
            {
                responseParts = [MessagePart.FromString(result.ToString() ?? "Ok but (string)result is null.")];
            }

            sender.Channel.SendMessage(new SendingMessage
            {
                Type = (int)MessageType.ChatMessage,
                Receiver = new MessageTarget
                {
                    GroupId = sender.GroupId,
                    UserId = sender.UserId
                },
                Content = new MessageContent
                {
                    Id = "",
                    Parts = responseParts
                }
            });
        }

    }
}
