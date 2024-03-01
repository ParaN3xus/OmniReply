using Newtonsoft.Json;
using System.Dynamic;
using OmniReply.MessageObjects;
using OmniReply.Utils.Config;
using static OmniReply.CommonUtils.Paths;
using static OmniReply.Core.Channel;
using static OmniReply.Utils.Exceptions;

namespace OmniReply.Core
{
    public class Session
    {
        public static readonly List<Session> sessions = [];

        public string SessionId;
        public bool IsGroup;
        public List<Plugin> enabledPlugins = [];

        private string storageFolder;
        private SessionConfig sessionConfig;

        private RemoteSandBox sandBox;

        public Session(string sessionId, bool isGroup)
        {
            sessions.Add(this);

            SessionId = sessionId;
            IsGroup = isGroup;

            var path = SessionsFolder + "/" + (isGroup ? "g/" : "");
            path += sessionId;

            storageFolder = path;

            InitFromFolder();
        }

        private void InitFromFolder()
        {
            if (!Directory.Exists(storageFolder))
            {
                Directory.CreateDirectory(storageFolder);
            }

            if (!File.Exists(storageFolder + "/sessionData.json"))
            {
                File.WriteAllText(storageFolder + "/sessionData.json", "{}");
            }

            if (!File.Exists(SessionsFolder + "/globalData.json"))
            {
                File.WriteAllText(SessionsFolder + "/globalData.json", "{}");
            }

            if (!File.Exists(storageFolder + "/sessionConfig.json"))
            {
                File.WriteAllText(storageFolder + "/sessionConfig.json", """
                    {
                        "disabled": [
                        ]
                    }
                    """);
            }

            // enabled plugins
            var sessionConfigJson = File.ReadAllText(storageFolder + "/sessionConfig.json");
            sessionConfig = JsonConvert.DeserializeObject<SessionConfig>(sessionConfigJson)!;
            sessionConfig.Path = storageFolder + "/sessionConfig.json";

            enabledPlugins = Plugin.Plugins.Where(p => !sessionConfig.DisabledPlugins.Any(d => d == p.Name)).ToList();


            // init code
            var references = new HashSet<string>();
            var usingNamespaces = new HashSet<string>();
            var usingStaticClasses = new HashSet<string>();

            var pluginCode = string.Empty;

            foreach (var plugin in enabledPlugins)
            {
                references.UnionWith(plugin.References);
                usingNamespaces.UnionWith(plugin.UsingNamespaces);
                usingStaticClasses.UnionWith(plugin.UsingStaticClasses);

                pluginCode += plugin.Code + Environment.NewLine;
            }

            var initCode = string.Empty;

            foreach (var s in usingStaticClasses)
            {
                initCode += "using static " + s + Environment.NewLine;
            }

            initCode += pluginCode;

            try
            {
                sandBox = new RemoteSandBox((IsGroup ? "g/" : "") + SessionId, initCode, [.. references], [.. usingNamespaces]);
            }
            catch (Exception e)
            {
                sessions.Remove(this);
                //sandBox.Dispose();
                throw e;
            }
        }

        public object? RunCode(string code, bool isTimeout = true)
        {
            object? res = null;

            try
            {
                res = sandBox.RunAsync(code[1..], isTimeout).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                res = e.Message;
                if (e is SandBoxTimeoutException)
                {
                    sessions.Remove(this);
                }
            }

            if (res == null)
            {
                return "OK!";
            }

            return res;
        }

        public object? RunPluginCommand(ReceivedChatMessage msg)
        {
            foreach (var plugin in enabledPlugins)
            {
                string? matched = plugin.MatchFunction(msg.Content.ToString());
                if (matched != string.Empty)
                {
                    var parameter = "JsonConvert.DeserializeObject<ReceivedChatMessage>(\"\"\"" + Environment.NewLine +
                        $"{JsonConvert.SerializeObject(msg)}" + Environment.NewLine +
                    "\"\"\")";

                    var res = RunCode($"${matched}({parameter})", false);

                    if(res == null)
                    {
                        return "OK!";
                    }
                    return res;
                }
            }

            return null;
        }


        public bool isUserBanned(string id)
        {
            return sessionConfig.BannedUser.Contains(id);
        }

        public void Remove()
        {
            sessions.Remove(this);
        }

        public void Reload()
        {
            Remove();
            _ = new Session(SessionId, IsGroup);
        }

        public void Reset()
        {
            sessions.Remove(this);
            Directory.Delete(storageFolder, true);
            Reload();
        }

        public bool UpdatePluginSettings(string pluginName, bool enableStatus)
        {
            bool reload = false;
            bool valid = false;

            foreach (var plugin in Plugin.Plugins)
            {
                if (plugin.Name == pluginName)
                {
                    valid = true;
                }
            }

            if (!valid)
            {
                return false;
            }

            if (enableStatus)
            {
                if (sessionConfig.DisabledPlugins.Contains(pluginName))
                {
                    sessionConfig.DisabledPlugins.Remove(pluginName);
                    reload = true;
                }
            }
            else
            {
                if (!sessionConfig.DisabledPlugins.Contains(pluginName))
                {
                    sessionConfig.DisabledPlugins.Add(pluginName);
                    reload = true;
                }
            }

            if (reload)
            {
                Reload();
            }

            return true;
        }

        private static void BanSession(string id, bool ban)
        {
            string rawid = id;
            if (ban)
            {
                bool isGroup = false;

                if (id.StartsWith("g/"))
                {
                    id = id[2..];
                    isGroup = true;
                }

                var session = sessions.FirstOrDefault(s => s.SessionId == id && s.IsGroup == isGroup);

                if (session != null)
                {
                    sessions.Remove(session);
                }

                GlobalConfig.globalConfig.BannedSessions.Add(rawid);
            }
            else
            {
                GlobalConfig.globalConfig.BannedSessions.Remove(rawid);
            }
        }

        private void Ban(string v, bool ban)
        {
            if(ban)
            {
                sessionConfig.BannedUser.Add(v);
            }
            else
            {
                sessionConfig.BannedUser.Remove(v);
            }
        }

        public string RunCommand(string msgText, MessageOrigin sender)
        {
            Queue<string> q = new Queue<string>(msgText[1..].Split(' '));

            Dictionary<string, string> args = new();

            if (q.Count == 0)
            {
                return "Invalid command!";
            }

            args["command"] = q.Dequeue();

            while(q.Count > 0)
            {
                var arg = q.Dequeue();
                if (arg.StartsWith('-'))
                {
                    args[arg[1..]] = q.Dequeue();
                }
                else
                {
                    if(args.ContainsKey("subcommand"))
                    {
                        return "Invalid command!";
                    }
                    args["subcommand"] = arg;
                }
            }

            return RunProcessedCommand(args, sender);
        }

        private string RunProcessedCommand(Dictionary<string, string> args, MessageOrigin sender)
        {
            switch (args["command"])
            {
                case "help":
                    return
@" - help
 - session
 - plugman
 - ban";
                case "session":
                    return RunSessionCommand(args, sender);
                case "plugman":
                    return RunPlugmanCommand(args, sender);
                case "ban":
                    return RunBanCommand(args, sender);
                default:
                    return string.Empty;
            }
        }

        private string RunSessionCommand(Dictionary<string, string> args, MessageOrigin sender)
        {
            if (args.Count == 1)
            {
                return
@" - list
 - reload
 - reset";
            }

            switch(args["subcommand"])
            {
                case "list":
                    return RunSessionListCommand(args, sender);
                case "reload":
                    return RunSessionReloadCommand(args, sender);
                case "reset":
                    return RunSessionResetCommand(args, sender);
                default:
                    return "Invalid command!";
            }
        }

        private string RunPlugmanCommand(Dictionary<string, string> args, MessageOrigin sender)
        {
            if (args.Count == 1)
            {
                return
@" - list
 - reload
 - disable
 - enable";
            }

            switch (args["subcommand"])
            {
                case "list":
                    return RunPlugmanListCommand(args, sender);
                case "reload":
                    return RunPlugmanReloadCommand(args, sender);
                case "disable":
                    return RunPlugmanXableCommand(args, sender);
                case "enable":
                    return RunPlugmanXableCommand(args, sender);
                default:
                    return "Invalid command!";
            }
        }

        private string RunBanCommand(Dictionary<string, string> args, MessageOrigin sender)
        {
            if (args.Count == 1)
            {
                return
@" - user
 - session";
            }

            switch (args["subcommand"])
            {
                case "user":
                    return RunBanUserCommand(args, sender);
                case "session":
                    return RunBanSessionCommand(args, sender);
                default:
                    return "Invalid command!";
            }
        }

        private string RunBanSessionCommand(Dictionary<string, string> args, MessageOrigin sender)
        {
            if (!GlobalConfig.globalConfig.Admins.Contains(sender.UserId))
            {
                return "Permission denied!";
            }

            bool ban = true;

            if(args.ContainsKey("ban"))
            {
                ban = args["ban"] == "true";
            }

            if (!args.ContainsKey("s"))
            {
                BanSession((IsGroup ? "g/" : "") + SessionId, ban);
            }
            else
            {
                BanSession(args["s"], ban);
            }

            return "OK!";
        }

        private string RunBanUserCommand(Dictionary<string, string> args, MessageOrigin sender)
        {
            var gconfig = GlobalConfig.globalConfig;

            if (!gconfig.Admins.Contains(sender.UserId))
            {
                return "Permission denied!";
            }

            bool ban = true;

            if (args.ContainsKey("ban"))
            {
                ban = args["ban"] == "true";
            }

            if (!args.ContainsKey("s"))
            {
                Ban(args["id"], ban);
            }
            else
            {
                var isgroup = false;
                if (args["s"].StartsWith("g/"))
                {
                    args["s"] = args["s"][2..];
                    isgroup = true;
                }
                var session = sessions.FirstOrDefault(s => s.SessionId == args["s"] && s.IsGroup == isgroup);

                if (session == null)
                {
                    return "Session not found!";
                }

                session.Ban(args["id"], ban);
            }

            return "OK!";
        }

        private string RunSessionListCommand(Dictionary<string, string> args, MessageOrigin sender)
        {
            if(!GlobalConfig.globalConfig.Admins.Contains(sender.UserId))
            {
                return "Permission denied!";
            }

            string s = string.Empty;
            foreach (var session in sessions)
            {
                s += (session.IsGroup ? "g/" : "") + session.SessionId;
                if (session != sessions.Last())
                {
                    s += Environment.NewLine;
                }
            }
            return s;
        }

        private string RunSessionReloadCommand(Dictionary<string, string> args, MessageOrigin sender)
        {
            if (args.Count == 2)
            {
                Reload();
                return "OK!";
            }
            else
            {
                if (!GlobalConfig.globalConfig.Admins.Contains(sender.UserId))
                {
                    return "Permission denied!";
                }

                var isgroup = false;
                if (args["s"].StartsWith("g/"))
                {
                    args["s"] = args["s"][2..];
                    isgroup = true;
                }
                var session = sessions.FirstOrDefault(s => s.SessionId == args["s"] && s.IsGroup == isgroup);

                if (session == null)
                {
                    return "Session not found!";
                }
                session.Reload();

                return "OK!";
            }
        }

        private string RunSessionResetCommand(Dictionary<string, string> args, MessageOrigin sender)
        {
            if (!GlobalConfig.globalConfig.Admins.Contains(sender.UserId))
            {
                return "Permission denied!";
            }

            if (args.Count == 2)
            {
                Reset();
                return "OK!";
            }
            else
            {
                var isgroup = false;
                if (args["s"].StartsWith("/g"))
                {
                    args["s"] = args["s"][2..];
                    isgroup = true;
                }
                var session = sessions.FirstOrDefault(s => s.SessionId == args["s"] && s.IsGroup == isgroup);

                if (session == null)
                {
                    return "Session not found!";
                }
                session.Reset();
                return "OK!";
            }
        }

        private string RunPlugmanListCommand(Dictionary<string, string> args, MessageOrigin sender)
        {
            string s = string.Empty;
            foreach (var plugin in Plugin.Plugins)
            {
                s += plugin.Name;
                if (plugin != Plugin.Plugins.Last())
                {
                    s += Environment.NewLine;
                }
            }
            return s;
        }

        private string RunPlugmanReloadCommand(Dictionary<string, string> args, MessageOrigin sender)
        {
            if (!GlobalConfig.globalConfig.Admins.Contains(sender.UserId))
            {
                return "Permission denied!";
            }

            if (args.Count == 2)
            {
                return "Invaild plugin to reload!";
            }

            if (args["n"] == "*")
            {
                Plugin.InitPlugins();
                return "OK!";
            }
            else
            {
                var plugin = Plugin.Plugins.FirstOrDefault(p => p.Name == args["n"]);
                if (plugin == null)
                {
                    return "Plugin not found!";
                }
                plugin.Reload();
                return "OK!";
            }
        }

        private string RunPlugmanXableCommand(Dictionary<string, string> args, MessageOrigin sender)
        {
            if (!GlobalConfig.globalConfig.Admins.Contains(sender.UserId))
            {
                return "Permission denied!";
            }

            if (args.Count == 2)
            {
                return "Invaild plugin to disable!";
            }

            Session session;

            if (args.ContainsKey("s"))
            {
                // find the session and update the plugin settings
                var isgroup = false;
                if (args["s"].StartsWith("g/"))
                {
                    args["s"] = args["s"][2..];
                    isgroup = true;
                }
                session = sessions.FirstOrDefault(s => s.SessionId == args["s"] && s.IsGroup == isgroup);

                if (session == null)
                {
                    return "Session not found!";
                }
            }
            else
            {
                session = this;
            }

            if (!session.UpdatePluginSettings(args["n"], args["subcommand"] == "enable"))
            {
                return "Plugin not found!";
            }
            return "OK!";
        }
    }
}

