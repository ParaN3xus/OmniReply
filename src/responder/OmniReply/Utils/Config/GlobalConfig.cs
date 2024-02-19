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
        public static GlobalConfig globalConfig = JsonConvert.DeserializeObject<GlobalConfig>(File.ReadAllText(Paths.ConfigPath))!;

        [JsonProperty("admins")]
        private List<string> admins = [];

        [JsonProperty("banned_sessions")]
        private ObservableCollection<string> bannedSessions = [];


        public List<string> Admins
        {
            get { return admins; }
        }

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
            File.WriteAllText(File.ReadAllText(Paths.ConfigPath), JsonConvert.SerializeObject(this));
        }
    }
}
