﻿using GHTMRM.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using static GHTMRM.Utils.Log;
using static GHTMRM.Utils.Paths;

namespace GHTMRM.Core
{
    public class Plugin
    {
        public static List<Plugin> Plugins = [];

        public string Name;
        public string Code;

        public HashSet<string> References;
        public HashSet<string> UsingNamespaces;
        public HashSet<string> UsingStaticClasses;
        public List<Tuple<string, string>> RegFuncPairs = [];

        private string PluginDir;

        public Plugin(string pluginDir)
        {
            var config = JsonConvert.DeserializeObject<ConfigObjects.PluginConfig>(File.ReadAllText(pluginDir + "/config.json"))!;

            Name = config.Name;

            Code = File.ReadAllText(pluginDir + "/code.cs");

            References = config.References.ToHashSet<string>();
            UsingNamespaces = config.UsingNamespaces.ToHashSet<string>();
            UsingStaticClasses = config.UsingStaticClasses.ToHashSet<string>();
            RegFuncPairs = config.RegFuncPairs.Select(x => new Tuple<string, string>(x.Regex, x.Function)).ToList();

            PluginDir = pluginDir;

            Plugins.Add(this);
        }

        public void Reload()
        {
            Plugins.Remove(this);

            foreach(var session in Session.sessions)
            {
                if(session.enabledPlugins.Contains(this))
                {
                    session.Reload();
                }
            }

            string pluginDir = new(PluginDir);
            Plugins.Add(new Plugin(pluginDir));
        }

        public static void InitPlugins()
        {
            Plugins.Clear();
            var pluginDirs = Directory.EnumerateDirectories(PluginsFolder);

            if (OperatingSystem.IsWindows())
            {
                pluginDirs = pluginDirs.Select(x => x.Replace("\\", "/"));
            }
                            
            foreach (var pluginDir in pluginDirs)
            {
                if (!File.Exists(pluginDir + "/config.json"))
                {
                    WriteLog(pluginDir + "/config.json not found", LogLevel.Warning);
                    continue;
                }

                if (!File.Exists(pluginDir + "/code.cs"))
                {
                    WriteLog(pluginDir + "/code.cs not found", LogLevel.Warning);
                    continue;
                }

                try
                {
                    _ = new Plugin(pluginDir);
                }
                catch (Exception ex)
                {
                    WriteLog(pluginDir + " failed to load: " + ex.Message, LogLevel.Warning);
                    continue;
                }
            }
        }

        public string MatchFunction(string msg)
        {
            if(msg == string.Empty)
            {
                return msg;
            }

            foreach (var pair in RegFuncPairs)
            {
                if (Regex.Match(msg, pair.Item1).Length == msg.Length)
                {
                    return pair.Item2;
                }
            }

            return string.Empty;
        }
    }
}
