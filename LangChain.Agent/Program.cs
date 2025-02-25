using IntelligentDataAgent.Core.Interfaces;
using IntelligentDataAgent.Core.Models;
using IntelligentDataAgent.Crawlers;
using IntelligentDataAgent.Elasticsearch;
using IntelligentDataAgent.Inference;
using IntelligentDataAgent.Processing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nest;
using IntelligentDataAgent.Core;
using System;
using System.Threading.Tasks;

namespace IntelligentDataAgent
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            await host.RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    // 绑定配置
                    var configuration = hostContext.Configuration;
                    var elasticsearchSettings = configuration.GetSection("ElasticsearchSettings").Get<ElasticsearchSettings>();
                    var crawlerSettings = configuration.GetSection("CrawlerSettings").Get<CrawlerSettings>();
                    var inferenceSettings = configuration.GetSection("InferenceSettings").Get<InferenceSettings>();
                    
                    services.Configure<ElasticsearchSettings>(configuration.GetSection("ElasticsearchSettings"));
                    services.Configure<CrawlerSettings>(configuration.GetSection("CrawlerSettings"));
                    services.Configure<InferenceSettings>(configuration.GetSection("InferenceSettings"));
                    services.Configure<List<ScheduledCrawl>>(configuration.GetSection("ScheduledCrawls"));

                    // 注册 HttpClient
                    services.AddHttpClient<IWebCrawler, WebCrawler>(client =>
                    {
                        client.DefaultRequestHeaders.Add("User-Agent", crawlerSettings.UserAgent);
                        client.Timeout = TimeSpan.FromSeconds(crawlerSettings.RequestTimeout);
                    });
                    
                    services.AddHttpClient<IApiCrawler, ApiCrawler>(client =>
                    {
                        client.DefaultRequestHeaders.Add("User-Agent", crawlerSettings.UserAgent);
                        client.Timeout = TimeSpan.FromSeconds(crawlerSettings.RequestTimeout);
                    });

                    // 注册核心服务
                    services.AddSingleton<IElasticClient>(sp =>
                    {
                        var connectionSettings = new ConnectionSettings(new Uri(elasticsearchSettings.Urls[0]))
                            .DefaultIndex(elasticsearchSettings.DefaultIndex);
                            
                        if (!string.IsNullOrEmpty(elasticsearchSettings.Username) && 
                            !string.IsNullOrEmpty(elasticsearchSettings.Password))
                        {
                            connectionSettings = connectionSettings.BasicAuthentication(
                                elasticsearchSettings.Username, 
                                elasticsearchSettings.Password);
                        }
                        
                        return new ElasticClient(connectionSettings);
                    });

                    // 注册数据抓取服务
                    services.AddSingleton<ICrawlerManager, CrawlerManager>();
                    services.AddTransient<IWebCrawler, WebCrawler>();
                    services.AddTransient<IApiCrawler, ApiCrawler>();

                    // 注册数据处理服务
                    services.AddSingleton<IDataProcessor, DataProcessor>();
                    services.AddTransient<ITextExtractor, TextExtractor>();
                    services.AddTransient<IDataNormalizer, DataNormalizer>();

                    // 注册Elasticsearch服务
                    services.AddSingleton<IElasticsearchService, ElasticsearchService>();
                    services.AddTransient<IIndexManager, IndexManager>();
                    services.AddTransient<ISearchService, SearchService>();

                    // 注册推理模型服务
                    services.AddSingleton<IInferenceModelService, InferenceModelService>();
                    
                    // 注册Agent协调器
                    services.AddHostedService<AgentCoordinator>();
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.AddDebug();
                });
    }
}
