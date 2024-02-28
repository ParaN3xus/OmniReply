namespace OmniReply.CommonUtils.SandBoxTransferTypes
{
    public class CommonResponse
    {
        public enum ResponseType
        {
            Null,
            String,
            MessageParts,
            Exception
        }
        public static CommonResponse FromException(Exception e)
        {
            return new CommonResponse
            {
                Type = (int)ResponseType.Exception,
                Data = e.Message
            };
        }

        public static readonly CommonResponse OkWithNull = new()
        {
            Type = (int)ResponseType.Null,
            Data = null
        };

        public int Type;
        public object? Data;
    }
}
