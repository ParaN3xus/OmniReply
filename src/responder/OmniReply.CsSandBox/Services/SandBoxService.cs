using Newtonsoft.Json;
using OmniReply.CommonUtils;
using OmniReply.CommonUtils.SandBoxTransferTypes;
using OmniReply.CsSandBox.Core;
using System.Dynamic;

namespace OmniReply.CsSandBox.Services
{
    public class SandBoxService
    {
        public SandBox SandBox;
        public string SessionId;
        public string Key;
        public SandBoxGlobals Globals;

        private ExpandoObject sessionData;
        private ExpandoObject globalData;


        private string storageFolder;

        public SandBoxService(string key)
        {
            Key = key;
        }

        public void Init(InitRequest initData)
        {
            storageFolder = Paths.SessionsFolder + '/' + initData.SessionId;

            sessionData = JsonConvert.DeserializeObject<ExpandoObject>(File.ReadAllText(storageFolder + "/sessionData.json"))!;
            globalData = JsonConvert.DeserializeObject<ExpandoObject>(File.ReadAllText(Paths.SessionsFolder + "/globalData.json"))!;

            Globals = new SandBoxGlobals(sessionData, globalData);

            SandBox = new SandBox(initData, Globals);
        }
    }

}
