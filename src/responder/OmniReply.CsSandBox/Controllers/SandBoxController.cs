using Microsoft.AspNetCore.Mvc;
using OmniReply.CommonUtils.SandBoxTransferTypes;
using OmniReply.CsSandBox.Services;
using OmniReply.MessageObjects;

namespace OmniReply.CsSandBox.Controllers
{
    [ApiController]
    [Route("")]
    public class SandBoxController : ControllerBase
    {
        private readonly SandBoxService _sandBoxService;

        public SandBoxController(SandBoxService runService)
        {
            _sandBoxService = runService;
        }

        [HttpGet("run")]
        public async Task<CommonResponse> Get(string key, string code)
        {
            if(_sandBoxService.SandBox == null)
            {
                return CommonResponse.FromException(new Exception("Not inited yet!"));
            }

            if(key != _sandBoxService.Key)
            {
                // just kidding
                return CommonResponse.OkWithNull;
            }

            try
            {
                var result = await _sandBoxService.SandBox.RunAsync(code);

                if(result == null)
                {
                    return CommonResponse.OkWithNull;
                }

                if(result is List<MessagePart>)
                {
                    return new CommonResponse
                    {
                        Type = (int)CommonResponse.ResponseType.MessageParts,
                        Data = (List<MessagePart>)result
                    };
                }

                return new CommonResponse
                {
                    Type = (int)CommonResponse.ResponseType.String,
                    Data = result.ToString()
                };
            }
            catch (Exception e)
            {
                return CommonResponse.FromException(e);
            }
        }


        [HttpPost("init")]
        public async Task<CommonResponse> InitSandBox([FromBody] InitRequest initData)
        {
            if(initData.Key != _sandBoxService.Key)
            {
                // just kidding
                return CommonResponse.OkWithNull;
            }

            if(_sandBoxService.SandBox != null)
            {
                return CommonResponse.FromException(new Exception("Already inited!"));
            }

            try
            {
                _sandBoxService.Init(initData);
            }
            catch (Exception e)
            {
                return CommonResponse.FromException(e);
            }

            return CommonResponse.OkWithNull;
        }
    }

}
