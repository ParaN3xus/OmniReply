using Newtonsoft.Json;
using System.Text;

namespace OmniReply.MessageObjects
{
    public enum MessageType
    {
        GiveID = 0,
        ChatMessage = 1
    }

    public enum MessagePartType
    {
        Text = 0,
        Image = 1,
        Record = 2
    }

    public class ReceivedMessage
    {
        [JsonProperty("type")]
        public int Type { get; set; }

    }

    public class ReceivedGiveIDMessage : ReceivedMessage
    {

        [JsonProperty("id")]
        public required string Id { get; set; }
    }

    public class ReceivedChatMessage : ReceivedMessage
    {

        [JsonProperty("sender")]
        public required MessageTarget Sender { get; set; }

        [JsonProperty("content")]
        public required MessageContent Content { get; set; }
    }

    public class SendingMessage
    {
        [JsonProperty("type")]
        public int Type { get; set; }

        [JsonProperty("receiver")]
        public required MessageTarget Receiver { get; set; }

        [JsonProperty("content")]
        public required MessageContent Content { get; set; }
    }

    public class MessageTarget
    {
        [JsonProperty("group_id")]
        public string? GroupId { get; set; }

        [JsonProperty("user_id")]
        public required string UserId { get; set; }
    }

    public class MessageContent
    {
        [JsonProperty("id")]
        public string? Id { get; set; }

        [JsonProperty("parts")]
        public required List<MessagePart> Parts { get; set; }

        public static string ConvertToString(List<MessagePart> parts)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var part in parts)
            {
                if (part.Type == (int)MessagePartType.Text)
                    sb.Append(part.Data);
            }
            return sb.ToString();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var part in Parts)
            {
                if (part.Type == (int)MessagePartType.Text)
                    sb.Append(part.Data);
            }
            return sb.ToString();
        }
    }

    public class MessagePart
    {
        [JsonProperty("type")]
        public int Type { get; set; }

        [JsonProperty("data")]
        public required string Data { get; set; }

        public MessagePart(MessagePartType type)
        {
            Type = (int)type;
        }

        public static MessagePart FromString(string data)
        {
            return new MessagePart(MessagePartType.Text) { Data = data };
        }
    }
}
