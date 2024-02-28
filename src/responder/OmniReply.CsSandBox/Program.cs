using Newtonsoft.Json.Serialization;
using OmniReply.CsSandBox.Services;

namespace OmniReply.CsSandBox
{
    public class Program
    {
        public static int Main(string[] args)
        {
            // port, key
            if (args.Length != 2)
            {
                return -1;
            }

            var port = args[0];
            var key = args[1];

            var builder = WebApplication.CreateBuilder();

            // Add services to the container.
            builder.Services.AddSingleton(new SandBoxService(key));
            builder.Services.AddControllers()
                .AddNewtonsoftJson();

            builder.WebHost.UseUrls($"http://localhost:{port}");

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            app.UseAuthorization();
            app.MapControllers();
            app.Run();

            return 0;
        }

    }
}
