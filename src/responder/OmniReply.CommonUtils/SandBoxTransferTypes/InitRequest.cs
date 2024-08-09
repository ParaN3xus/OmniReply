using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OmniReply.CommonUtils.SandBoxTransferTypes
{
    public class InitRequest
    {
        [JsonProperty("key")]
        public string Key;

        [JsonProperty("session_id")]
        public string SessionId;

        [JsonProperty("init_code")]
        public string InitCode;

        [JsonProperty("references")]
        public List<string> References;

        [JsonProperty("using_namespaces")]
        public List<string> UsingNamespaces;
    }
    
    public class RunRequest
    {
        [JsonProperty("key")]
        public string Key;

        [JsonProperty("code")]
        public string Code;
    }
}
