using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniReply.Utils.Config
{
    public class GlobalConfig
    {
        public static GlobalConfig globalConfig = JsonConvert.DeserializeObject<GlobalConfig>(File.ReadAllText(Paths.ConfigPath))!;

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
