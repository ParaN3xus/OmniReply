using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniReply.Utils
{
    public static class ConfigObjects
    {
        public class PluginConfig
        {
            public class RegFuncPair
            {
                [JsonProperty("regex")]
                public required string Regex;

                [JsonProperty("function")]
                public required string Function;
            }

            [JsonProperty("name")]
            public required string Name;

            [JsonProperty("using_namespaces")]
            public List<string> UsingNamespaces = [];

            [JsonProperty("using_static_classes")]
            public List<string> UsingStaticClasses = [];

            [JsonProperty("references")]
            public List<string> References = [];

            [JsonProperty("regexps")]
            public List<RegFuncPair> RegFuncPairs = [];
        }

        public class SessionConfig
        {
            [JsonProperty("disabled_plugins")]
            public List<string> DisabledPlugins = [];

            [JsonProperty("banned_user")]
            public List<string> BannedUser = [];
        }


        public static GlobalConfig globalConfig = JsonConvert.DeserializeObject<GlobalConfig>(File.ReadAllText(Paths.ConfigPath))!;
        public class GlobalConfig
        {
            [JsonProperty("admins")]
            private List<string> admins = [];

            [JsonProperty("banned_sessions")]
            private List<string> bannedSessions = [];


            public List<string> Admins
            {
                get { return admins; }
            }

            public List<string> BannedSessions
            {
                get { return bannedSessions; }
                set
                {
                    bannedSessions = value;
                    File.WriteAllText(File.ReadAllText(Paths.ConfigPath), JsonConvert.SerializeObject(this));
                }
            }
        }

    }
}
