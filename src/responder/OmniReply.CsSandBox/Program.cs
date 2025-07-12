﻿using Newtonsoft.Json.Serialization;
using OmniReply.CsSandBox.Services;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;

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
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.Limits.MaxRequestBodySize = 52428800; // 50 MB
                options.Limits.MaxRequestLineSize = 52428800;
                options.Limits.MaxRequestHeadersTotalSize = 52428800;
                options.Limits.MaxRequestBufferSize = 52428801;
            });

            builder.Services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 52428800;
            });

            // Add services to the container.
            builder.Services.AddSingleton(new SandBoxService(key));
            builder.Services.AddControllers()
                .AddNewtonsoftJson();

            builder.WebHost.UseUrls($"http://localhost:{port}");

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            app.UseAuthorization();
            app.MapControllers();

            try
            {
                app.Run();
            }
            catch (Exception e)
            {
                if(e.InnerException is AddressInUseException)
                {
                    Console.WriteLine("[PORTINUSE]");
                    return -2;
                }
                else
                {
                    Console.WriteLine("[FAILED]");
                    return -3;
                }
            }

            return 0;
        }

    }
}
