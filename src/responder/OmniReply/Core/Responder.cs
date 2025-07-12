using OmniReply.MessageObjects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static OmniReply.Core.Channel;
using static OmniReply.CommonUtils.Log;
using OmniReply.Utils.Config;

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
            // construct session id
            var sessionId = sender.UserId;
            var isGroup = false;

            if(sender.GroupId != null)
            {
                sessionId = sender.GroupId;
                isGroup = true;
            }

            // banned session
            if(GlobalConfig.globalConfig.BannedSessions.Contains((isGroup ? "g/" : "") + sessionId))
            {
                WriteLog($"Banned session: {sessionId}", LogLevel.Info);
                return;
            }


            // respond
            var messageParts = message.Content.Parts;
            var msgText = MessageContent.ConvertToString(messageParts);
            object? result = null;
            try
            {
                // search corresponding session, if not found, create a new one
                var session = Session.sessions.FirstOrDefault(s => s.SessionId == sessionId && s.IsGroup == isGroup);
                if (session == null)
                {
                    WriteLog($"Session not found: {sessionId}, creating...", LogLevel.Info);
                    session = new Session(sessionId, isGroup);
                }

                // banned user
                if (session.isUserBanned(sender.UserId))
                {
                    WriteLog($"Banned User: {sender.UserId}", LogLevel.Info);
                    return;
                }

                // run the code
                if(msgText.StartsWith('$'))
                {
                    result = session.RunCode(msgText, true);
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
                result = e.Message + e.StackTrace;
            }

            // check if result is invaild
            if ((result == null) || (result.ToString() == string.Empty && msgText.StartsWith('#')))
            {
                WriteLog("Invalid result", LogLevel.Warning);
                return;
            }

            // send the result
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
