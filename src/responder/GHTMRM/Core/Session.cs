using GHTMRM.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GHTMRM.Utils.Paths;
using GHTMRM.Core.CsScript;
using System.Dynamic;
using static GHTMRM.Utils.ConfigObjects;
using static System.Collections.Specialized.BitVector32;
using GHTMRM.MessageObjects;
using static GHTMRM.Core.Channel;

namespace GHTMRM.Core
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

        public Session(string sessionId, bool isGroup)
        {
            this.SessionId = sessionId;
            this.isGroup = isGroup;

            var path = SessionsFolder + "/" + (isGroup ? "g/" : "");
            path += sessionId;

            storageFolder = path;

            InitFromFolder();

            sessions.Add(this);
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

            if (!File.Exists(storageFolder + "/code.cs"))
            {
                File.WriteAllText(storageFolder + "/code.cs", "");
            }

            // enabled plugins
            var sessionConfigJson = File.ReadAllText(storageFolder + "/sessionConfig.json");
            var sessionConfig = JsonConvert.DeserializeObject<SessionConfig>(sessionConfigJson)!;

            enabledPlugins = Plugin.Plugins.Where(p => !sessionConfig.Disabled.Any(d => d.Name == p.Name)).ToList();


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

        public object? RunCode(string code)
        {
            object? res = null;
            Thread scriptThread = new Thread(() =>
            {
                res = RunCodeProcess(code);
            });
            scriptThread.Start();


            if (!scriptThread.Join(TimeSpan.FromSeconds(7)))
            {
                scriptThread.Interrupt();
                throw new OperationCanceledException("Timeout!");
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
            var matched = string.Empty;
            foreach (var plugin in enabledPlugins)
            {
                matched = plugin.MatchFunction(msg.Content.ToString());
                if (matched != string.Empty)
                {
                    var parameter = "JsonConvert.DeserializeObject<ReceivedChatMessage>(\"\"\"" + Environment.NewLine +
                        $"{JsonConvert.SerializeObject(msg)}" + Environment.NewLine +
                    "\"\"\")";

                    var res = RunCode($"${matched}({parameter})");

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
        }

        public string RunCommand(string msgText, MessageOrigin sender)
        {
            if(sender.UserId!= "3072048468")
            {
                return "Permission denied!";
            }

            var command = msgText[1..];
            var args = command.Split(' ');

            if (args.Length == 0)
            {
                return 
@" - session
 - plugman";
            }

            if (args[0] == "session")
            {
                if (args.Length == 1)
                {
                    return
@" - list
 - reload
 - reset";
                }
                else if (args[1] == "list")
                {
                    string s = string.Empty;
                    foreach (var session in sessions)
                    {
                        s += (session.isGroup ? "g/" : "") + session.SessionId;
                        if(session != sessions.Last())
                        {
                            s += Environment.NewLine;
                        }
                    }
                    return s;
                }
                else if (args[1] == "reload")
                {
                    if(args.Length == 2)
                    {
                        Reload();
                        return "OK!";
                    }
                    else
                    {
                        var isgroup = false;
                        if (args[2].StartsWith("g/"))
                        {
                            args[2] = args[2][2..];
                            isgroup = true;
                        }
                        var session = sessions.FirstOrDefault(s => s.SessionId == args[2] && s.isGroup == isgroup);

                        if(session == null)
                        {
                            return "Session not found!";
                        }
                        session.Reload();

                        return "OK!";
                    }
                }
                else if (args[1] == "reset")
                {
                    if (args.Length == 2)
                    {
                        Reset();

                        return "OK!";
                    }
                    else
                    {
                        var isgroup = false;
                        if (args[2].StartsWith("/g"))
                        {
                            args[2] = args[2][2..];
                            isgroup = true;
                        }
                        var session = sessions.FirstOrDefault(s => s.SessionId == args[2] && s.isGroup == isgroup);

                        if (session == null)
                        {
                            return "Session not found!";
                        }
                        session.Reset();

                        _ = new Session(SessionId, isGroup);
                        return "OK!";
                    }
                }
                else
                {
                    return "Invalid command!";
                }
            }
            else if (args[0] == "plugman")
            {
                if (args.Length == 1)
                {
                    return
@" - list
 - reload
 - disable
 - enable";
                }

                if (args[1] == "list")
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
                else if (args[1] == "reload")
                {
                    if (args.Length == 2)
                    {
                        return "Invaild plugin to reload!";
                    }

                    if (args[2] == "all")
                    {
                        Plugin.InitPlugins();
                        return "OK!";
                    }
                    else
                    {
                        var plugin = Plugin.Plugins.FirstOrDefault(p => p.Name == args[2]);
                        if (plugin == null)
                        {
                            return "Plugin not found!";
                        }
                        plugin.Reload();
                        return "OK!";
                    }
                }
                else if (args[1] == "disable")
                {

                }
                else
                {
                    return "Invalid command!";
                }
            }

            return "Nothing returned!";
        }
    }
}

