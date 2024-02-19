using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniReply.Utils.Config
{
    public class SessionConfig
    {
        [JsonProperty("disabled_plugins")]
        public List<string> DisabledPlugins = [];

        [JsonProperty("banned_user")]
        public List<string> BannedUser = [];
    }
}
