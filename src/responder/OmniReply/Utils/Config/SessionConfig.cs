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
        public string Path = string.Empty;

        [JsonProperty("disabled_plugins")]
        private ObservableCollection<string> disabledPlugins = [];

        [JsonProperty("banned_user")]
        private ObservableCollection<string> bannedUser = [];

        public ObservableCollection<string> DisabledPlugins
        {
            get { return disabledPlugins; }
        }

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
            File.WriteAllText(Path, JsonConvert.SerializeObject(this));
        }
    }
}
