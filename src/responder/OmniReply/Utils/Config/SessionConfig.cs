using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniReply.Utils.Config
{
    public class SessionConfig
    {
        [JsonIgnore]
        public string Path = string.Empty;

        [JsonProperty("disabled_plugins")]
        private ObservableCollection<string> disabledPlugins = [];

        [JsonProperty("banned_user")]
        private ObservableCollection<string> bannedUser = [];

        [JsonIgnore]
        public ObservableCollection<string> DisabledPlugins
        {
            get { return disabledPlugins; }
        }

        [JsonIgnore]
        public ObservableCollection<string> BannedUser
        {
            get { return bannedUser; }
        }

        public SessionConfig()
        {
            disabledPlugins.CollectionChanged += (sender, e) => UpdateConfig();
            bannedUser.CollectionChanged += (sender, e) => UpdateConfig();
        }

        private void UpdateConfig()
        {
            if (Path != string.Empty)
            { 
                File.WriteAllText(Path, JsonConvert.SerializeObject(this));
            }
        }
    }
}
