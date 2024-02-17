using OmniReply.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static OmniReply.Utils.Paths;
using OmniReply.Core.CsScript;
using System.Dynamic;
using static OmniReply.Utils.ConfigObjects;
using OmniReply.MessageObjects;
using static OmniReply.Core.Channel;

namespace OmniReply.Core
{
    public class Session
    {
        public static readonly List<Session> sessions = [];

        public string SessionId;
        public bool isGroup;
        public List<Plugin> enabledPlugins = [];

        private CsSandBox sandBox;
        private SandBoxGlobals sandBoxGlobals;
        private string storageFolder;
        private SessionConfig sessionConfig;

        public Session(string sessionId, bool isGroup)
        {
            sessions.Add(this);

            this.SessionId = sessionId;
            this.isGroup = isGroup;

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

             enabledPlugins = Plugin.Plugins.Where(p => !sessionConfig.Disabled.Any(d => d == p.Name)).ToList();


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

            ExpandoObject sessionData = JsonConvert.DeserializeObject<ExpandoObject>(File.ReadAllText(storageFolder + "/sessionData.json"))!;
            ExpandoObject globalData = JsonConvert.DeserializeObject<ExpandoObject>(File.ReadAllText(SessionsFolder + "/globalData.json"))!;

            sandBoxGlobals = new SandBoxGlobals(sessionData, globalData);


            // banned type
            var bannedType = new List<Type>
                            {
                                //not allow to use System.Reflection.Module,System.AppDomain
                                typeof(System.Reflection.Module),
                                typeof(System.AppDomain),
                                typeof(System.Diagnostics.Process),
                                typeof(System.Threading.Thread),
                            };

            sandBox = new CsSandBox(initCode, references, bannedType, sandBoxGlobals, usingNamespaces.ToList());
        }

        public object? RunCode(string code, bool isTimeout = true)
        {
            object? res = null;
            Thread scriptThread = new Thread(() =>
            {
                res = RunCodeProcess(code);
            });
            scriptThread.Start();

            if (isTimeout)
            {
                if (!scriptThread.Join(TimeSpan.FromSeconds(16)))
                {
                    scriptThread.Interrupt();
                    throw new OperationCanceledException("Timeout!");
                }
            }
            else
            {
                scriptThread.Join();
            }

            var sessionDataJson = sandBoxGlobals.GetSessionDataJson();
            File.WriteAllText(storageFolder + "/sessionData.json", sessionDataJson);

            var globalDataJson = sandBoxGlobals.GetGlobalDataJson();
            File.WriteAllText(SessionsFolder + "/globalData.json", globalDataJson);


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

        private object? RunCodeProcess(string code)
        {
            object? res;
            try
            {
                res = sandBox.RunAsync(code[1..]).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                return e.Message;
            }
            return res;
        }

        public void Remove()
        {
            sessions.Remove(this);
        }

        public void Reload()
        {
            Remove();
            _ = new Session(SessionId, isGroup);
        }

        public void Reset()
        {
            sessions.Remove(this);
            Directory.Delete(storageFolder, true);
            Reload();
        }

        public bool UpdatePluginSettings(string pluginName, bool enableStatus)
        {
            bool updateConfig = false;
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
                if (sessionConfig.Disabled.Contains(pluginName))
                {
                    sessionConfig.Disabled.Remove(pluginName);
                    updateConfig = true;
                }
            }
            else
            {
                if (!sessionConfig.Disabled.Contains(pluginName))
                {
                    sessionConfig.Disabled.Add(pluginName);
                    updateConfig = true;
                }
            }

            if(updateConfig)
            {
                var sessionConfigJson = JsonConvert.SerializeObject(sessionConfig);
                File.WriteAllText(storageFolder + "/sessionConfig.json", sessionConfigJson);

                Reload();
            }

            return true;
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
 - plugman";
                case "session":
                    return RunSessionCommand(args, sender);
                case "plugman":
                    return RunPlugmanCommand(args, sender);
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

        private string RunSessionListCommand(Dictionary<string, string> args, MessageOrigin sender)
        {
            if(!GlobalConfigs.Admins.Contains(sender.UserId))
            {
                return "Permission denied!";
            }

            string s = string.Empty;
            foreach (var session in sessions)
            {
                s += (session.isGroup ? "g/" : "") + session.SessionId;
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
                if (!GlobalConfigs.Admins.Contains(sender.UserId))
                {
                    return "Permission denied!";
                }

                var isgroup = false;
                if (args["s"].StartsWith("g/"))
                {
                    args["s"] = args["s"][2..];
                    isgroup = true;
                }
                var session = sessions.FirstOrDefault(s => s.SessionId == args["s"] && s.isGroup == isgroup);

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
            if (!GlobalConfigs.Admins.Contains(sender.UserId))
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
                var session = sessions.FirstOrDefault(s => s.SessionId == args["s"] && s.isGroup == isgroup);

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
            if (!GlobalConfigs.Admins.Contains(sender.UserId))
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
            if (!GlobalConfigs.Admins.Contains(sender.UserId))
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
                session = sessions.FirstOrDefault(s => s.SessionId == args["s"] && s.isGroup == isgroup);

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

