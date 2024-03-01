using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OmniReply.CommonUtils;

namespace OmniReply.CsSandBox.Core
{
    public class SandBoxGlobals
    {
        internal ExpandoObject sessionData;
        internal ExpandoObject globalData;

        internal string sessionDataPath;
        internal string globalDataPath;

        internal SandBoxGlobals()
        {
            sessionData = new ExpandoObject();
            globalData = new ExpandoObject();
        }
        internal SandBoxGlobals(ExpandoObject sessionData, ExpandoObject globalData, string sessionId)
        {
            this.sessionData = sessionData;
            this.globalData = globalData;
            sessionDataPath = Paths.SessionsFolder + '/' + sessionId + "/sessionData.json";
            globalDataPath = Paths.SessionsFolder + "/globalData.json";
        }

        internal string GetSessionDataJson()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(sessionData);
        }

        internal string GetGlobalDataJson()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(globalData);
        }

        public object? GetSessionData(string key)
        {
            if (sessionData == null)
                return null;

            if(!((IDictionary<string, object>)sessionData).ContainsKey(key))
                return null;

            return ((IDictionary<string, object>)sessionData)[key];
        }

        public void SetSessionData(string key, object? value)
        {
            if (sessionData == null)
                sessionData = new ExpandoObject();

            if(JsonConvert.SerializeObject(value).Length + GetSessionDataJson().Length > 65536)
            {
                throw new Exception("Data too large");
            }

            ((IDictionary<string, object>)sessionData)[key] = value;
            File.WriteAllText(sessionDataPath, GetSessionDataJson());
        }

        public object? GetGlobalData(string key)
        {
            if (globalData == null)
                return null;
            return ((IDictionary<string, object>)globalData)[key];
        }

        public void SetGlobalData(string key, object? value)
        {
            if (globalData == null)
                globalData = new ExpandoObject();

            if (JsonConvert.SerializeObject(value).Length + GetGlobalDataJson().Length > 65536)
            {
                throw new Exception("Data too large");
            }

            ((IDictionary<string, object>)globalData)[key] = value;
            File.WriteAllText(globalDataPath, GetGlobalDataJson());
        }
    }
}
