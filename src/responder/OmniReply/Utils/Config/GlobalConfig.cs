using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniReply.Utils.Config
{
    public class GlobalConfig
    {
        public static GlobalConfig globalConfig = InitGlobalConfig();

        [JsonProperty("admins")]
        private List<string> admins = [];

        [JsonProperty("banned_sessions")]
        private ObservableCollection<string> bannedSessions = [];

        [JsonIgnore]
        public List<string> Admins
        {
            get { return admins; }
        }

        [JsonIgnore]
        public ObservableCollection<string> BannedSessions
        {
            get { return bannedSessions; }
        }

        public GlobalConfig()
        {
            bannedSessions.CollectionChanged += (sender, e) => UpdateConfig();
        }

        private void UpdateConfig()
        {
            File.WriteAllText(Paths.ConfigPath, JsonConvert.SerializeObject(this));
        }

        private static GlobalConfig InitGlobalConfig()
        {
            if(File.Exists(Paths.ConfigPath) == false)
            {
                File.WriteAllText(Paths.ConfigPath, "{}");
            }

            return JsonConvert.DeserializeObject<GlobalConfig>(File.ReadAllText(Paths.ConfigPath))!;
        }
    }
}
