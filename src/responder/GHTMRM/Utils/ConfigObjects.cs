using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GHTMRM.Utils
{
    public class ConfigObjects
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
            [JsonProperty("disabled")]
            public List<PluginConfig> Disabled = [];
        }
    }
}
