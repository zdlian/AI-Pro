using IntelligentDataAgent.Core.Interfaces;
using IntelligentDataAgent.Core.Models;
using IntelligentDataAgent.Crawlers;
using IntelligentDataAgent.Elasticsearch;
using IntelligentDataAgent.Inference;
using IntelligentDataAgent.Processing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Nest;
using System;
using System.Collections.Generic;

namespace IntelligentDataAgent.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // 添加控制器
            services.AddControllers();

            // 添加Swagger
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            // 绑定配置
            services.Configure<ElasticsearchSettings>(Configuration.GetSection("ElasticsearchSettings"));
            services.Configure<CrawlerSettings>(Configuration.GetSection("CrawlerSettings"));
            services.Configure<InferenceSettings>(Configuration.GetSection("InferenceSettings"));
            services.Configure<List<CrawlSchedule>>(Configuration.GetSection("ScheduledCrawls"));

            // 注册HttpClient
            services.AddHttpClient();

            // 注册ElasticClient
            services.AddSingleton<IElasticClient>(sp =>
            {
                var settings = sp.GetRequiredService<IOptions<ElasticsearchSettings>>().Value;
                var connectionSettings = new ConnectionSettings(new Uri(settings.Urls[0]))
                    .DefaultIndex(settings.DefaultIndex);
                
                if (settings.EnableDebugMode)
                {
                    connectionSettings = connectionSettings.EnableDebugMode();
                }
                
                if (!string.IsNullOrEmpty(settings.Username) && !string.IsNullOrEmpty(settings.Password))
                {
                    connectionSettings = connectionSettings.BasicAuthentication(settings.Username, settings.Password);
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
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
} 