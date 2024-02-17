using System.Text;
using WatsonWebsocket;
using Newtonsoft.Json;
using OmniReply.Core;

namespace OmniReply
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            var responder = new Responder();

            var wsInterface = new WsInterface(responder);


            while (true)
            {
                Console.ReadLine();
            }
        }



    }
}
