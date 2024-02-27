using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniReply.Utils
{
    public static class Paths
    {

        public static string RootFolder = System.Diagnostics.Debugger.IsAttached ?
                                            Directory.GetParent(
                                                Directory.GetCurrentDirectory())!
                                                    .Parent!
                                                    .FullName
                                                    .Replace('\\', '/') + "/files" :
                                            "./files";


        public static string SessionsFolder = RootFolder + "/sessions";
        public static string PluginsFolder = RootFolder + "/plugins";

        public static string ConfigPath = RootFolder + "/config.json";

    }
}
