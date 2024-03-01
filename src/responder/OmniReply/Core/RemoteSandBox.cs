using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using OmniReply.CommonUtils;
using OmniReply.CommonUtils.SandBoxTransferTypes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static OmniReply.Utils.Exceptions;

namespace OmniReply.Core
{
    public class RemoteSandBox : IDisposable
    {
        private readonly string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        private string key;
        private int port;

        private Process sandBoxProcess;
        private HttpClient httpClient;

        public RemoteSandBox(string sessionId, string initCode, List<string> references, List<string> usingNamesapces)
        {
            var stringChars = new char[32];
            var random = new Random();

            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = chars[random.Next(chars.Length)];
            }

            key = new(stringChars);

        port:
            
            port = random.Next(10000, 65535);

            sandBoxProcess = new Process();
            sandBoxProcess.StartInfo.FileName = "OmniReply.CsSandBox" + (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : string.Empty);
            sandBoxProcess.StartInfo.Arguments = $"{port} {key}";
            sandBoxProcess.StartInfo.UseShellExecute = false;
            sandBoxProcess.StartInfo.RedirectStandardOutput = true;

            sandBoxProcess.ErrorDataReceived += (sender, e) =>
            {
                Log.WriteLog($"SandBox process err: {e.Data}", Log.LogLevel.Error);
            };

            sandBoxProcess.Start();

            string? s;
            bool flag = false;

            while (flag)
            {
                s = sandBoxProcess.StandardOutput.ReadLine();
                if(s == null)
                {
                    continue;
                }

                if (s.Contains("Content root path:"))
                {
                    flag = false;
                }
                else if (s == "[PORTINUSE]")
                {
                    sandBoxProcess.Kill();
                    goto port;
                }
                else if (s == "[FAILED]")
                {
                    sandBoxProcess.Kill();
                    throw new Exception("Failed to start the sandbox");
                }
            }

            sandBoxProcess.OutputDataReceived += (sender, e) =>
            {
                ;
            };
            
            httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri($"http://localhost:{port}");

            // wait until inited
            while(true)
            {
                try
                {
                    var response = httpClient.GetAsync("/run").GetAwaiter().GetResult();
                    break;
                }
                catch
                {
                    Thread.Sleep(50);
                }
            }   

            SendInitRequest(sessionId, initCode, references, usingNamesapces);
        }

        private void SendInitRequest(string sessionId, string initCode, List<string> references, List<string> usingNamesapces)
        {
            var response = httpClient.PostAsync("/init", new StringContent(JsonConvert.SerializeObject(new InitRequest
            {
                Key = key,
                SessionId = sessionId,
                InitCode = initCode,
                References = references,
                UsingNamespaces = usingNamesapces
            }), Encoding.UTF8, "application/json")).GetAwaiter().GetResult();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Failed to init the sandbox");
            }

            var result = JsonConvert.DeserializeObject<CommonResponse>(
                response.Content.ReadAsStringAsync().GetAwaiter().GetResult());

            if (result == null)
            {
                throw new Exception("Failed to init the sandbox");
            }
            if( result.Type == (int)CommonResponse.ResponseType.Exception)
            {
                throw new Exception(result.Data!.ToString());
            }
        }

        public async Task<object?> RunAsync(string code, bool isTimeout)
        {
            var cts = new CancellationTokenSource();
            if (isTimeout)
            {
                cts.CancelAfter(TimeSpan.FromSeconds(16));
            }

            HttpResponseMessage response;

            try
            {
                var url = $"/run?key={key}&code={System.Net.WebUtility.UrlEncode(code)}";
                response = await httpClient.GetAsync(url, cts.Token);
            }
            catch(TaskCanceledException)
            {
                sandBoxProcess.Kill();
                throw new SandBoxTimeoutException();
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Failed to run the code");
            }

            var result = JsonConvert.DeserializeObject<CommonResponse>(
                               await response.Content.ReadAsStringAsync());

            if (result == null)
            {
                throw new Exception("Failed to run the code");
            }

            if (result.Type == (int)CommonResponse.ResponseType.Exception)
            {
                throw new Exception(result.Data!.ToString());
            }

            if (result.Type == (int)CommonResponse.ResponseType.Null)
            {
                return null;
            }

            if (result.Type == (int)CommonResponse.ResponseType.String)
            {
                return result.Data!;
            }

            if (result.Type == (int)CommonResponse.ResponseType.MessageParts)
            {
                return result.Data!;
            }

            throw new Exception("Failed to run the code");
        }

        public void Dispose()
        {
            sandBoxProcess.Kill();
        }
    }
}
