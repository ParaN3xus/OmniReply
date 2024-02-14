using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GHTMRM.Utils.Log;
using WatsonWebsocket;
using GHTMRM.MessageObjects;

namespace GHTMRM.Core
{
    public class WsInterface
    {
        WatsonWsServer wsServer;
        Responder msgResponder;

        public WsInterface(Responder responder)
        {
            msgResponder = responder;
            
            wsServer = new WatsonWsServer("localhost", 8080, false);

            wsServer.ClientConnected += ClientConnected;
            wsServer.ClientDisconnected += ClientDisconnected;
            wsServer.MessageReceived += MessageReceived;

            wsServer.Start();
            WriteLog("WebSocket server started", LogLevel.Info);
        }

        void ClientConnected(object? sender, ConnectionEventArgs args)
        {
            WriteLog($"Client connected: {args.Client}", LogLevel.Info);
            Channel.channels.Add(new Channel(this, msgResponder, args.Client.Guid, "Untitled"));
        }

        void ClientDisconnected(object? sender, DisconnectionEventArgs args)
        {
            WriteLog($"Client disconnected: {args.Client}", LogLevel.Info);

            var channel = Channel.channels.Find(c => c.WsClientId == args.Client.Guid);

            if(channel == null)
            {
                WriteLog($"Channel not found for disconnected client: {args.Client.Guid}", LogLevel.Warning);
                return;
            }

            Channel.channels.Remove(channel);
        }

        void MessageReceived(object? sender, MessageReceivedEventArgs args)
        {
            var msgJson = Encoding.UTF8.GetString(args.Data);

            WriteLog($"Message received from {args.Client}: {msgJson[0..Math.Min(msgJson.Length, 64)]}", LogLevel.Debug);

            if (args.Data.Count < 1)
            {
                WriteLog("No data received", LogLevel.Warning);
                return;
            }

            ReceivedMessage msg = JsonConvert.DeserializeObject<ReceivedMessage>(Encoding.UTF8.GetString(args.Data))!;

            if (msg.Type == (int)MessageType.GiveID)
            {
                var giveIdMsg = JsonConvert.DeserializeObject<ReceivedGiveIDMessage>(Encoding.UTF8.GetString(args.Data))!;
                Channel channel = Channel.channels.Find(c => c.WsClientId == args.Client.Guid)!;

                if (channel == null)
                {
                    WriteLog($"GiveID message received from bad channel: {args.Client.Guid}", LogLevel.Warning);
                    return;
                }

                channel.Id = giveIdMsg.Id;
                WriteLog($"GiveID message received: {giveIdMsg.Id}", LogLevel.Info);
            }
            else if (msg.Type == (int)MessageType.ChatMessage)
            {
                var chatMsg = JsonConvert.DeserializeObject<ReceivedChatMessage>(Encoding.UTF8.GetString(args.Data))!;
                Channel channel = Channel.channels.Find(c => c.WsClientId == args.Client.Guid)!;

                if(channel == null)
                {
                    WriteLog($"Chat message get from bad channel: {args.Client.Guid}", LogLevel.Warning);
                    return;
                }

                if (chatMsg.Content.Parts.Count == 0)
                {
                    WriteLog($"Blank chat message received from {chatMsg.Sender.UserId}", LogLevel.Warning);
                    return;
                }

                channel.ReceivedChatMessage(new Channel.MessageOrigin 
                { 
                    Channel = channel, 
                    UserId = chatMsg.Sender.UserId, 
                    GroupId = chatMsg.Sender.GroupId 
                }, chatMsg);


                WriteLog($"Chat message received from {chatMsg.Sender.UserId}: ...{chatMsg.Content.Parts[0].Data[^Math.Min(chatMsg.Content.Parts[0].Data.Length, 16)..]}...", LogLevel.Info);
            }
        }

        public void SendMessage(Guid clientId, SendingMessage msg)
        {
            var json = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(msg));

            wsServer.SendAsync(clientId, json);
        }
    }
}
