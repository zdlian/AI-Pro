using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using IntelligentDataAgent.Core.Interfaces;
using IntelligentDataAgent.Core.Models;
using IntelligentDataAgent.Crawlers;
using IntelligentDataAgent.Elasticsearch;
using IntelligentDataAgent.Inference;
using IntelligentDataAgent.Processing;
using Microsoft.Extensions.Options;
using Nest;
using System;

namespace IntelligentDataAgent.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.AddDebug();
                });
    }
} 