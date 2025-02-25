using IntelligentDataAgent.Core.Interfaces;
using IntelligentDataAgent.Core.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IntelligentDataAgent.Core
{
    public class AgentCoordinator : BackgroundService
    {
        private readonly ICrawlerManager _crawlerManager;
        private readonly IDataProcessor _dataProcessor;
        private readonly IElasticsearchService _elasticsearchService;
        private readonly IInferenceModelService _inferenceModelService;
        private readonly ILogger<AgentCoordinator> _logger;
        private readonly List<ScheduledCrawl> _scheduledCrawls;
        private readonly ElasticsearchSettings _elasticsearchSettings;
        private readonly InferenceSettings _inferenceSettings;

        public AgentCoordinator(
            ICrawlerManager crawlerManager,
            IDataProcessor dataProcessor,
            IElasticsearchService elasticsearchService,
            IInferenceModelService inferenceModelService,
            IOptions<List<ScheduledCrawl>> scheduledCrawls,
            IOptions<ElasticsearchSettings> elasticsearchSettings,
            IOptions<InferenceSettings> inferenceSettings,
            ILogger<AgentCoordinator> logger)
        {
            _crawlerManager = crawlerManager;
            _dataProcessor = dataProcessor;
            _elasticsearchService = elasticsearchService;
            _inferenceModelService = inferenceModelService;
            _scheduledCrawls = scheduledCrawls.Value;
            _elasticsearchSettings = elasticsearchSettings.Value;
            _inferenceSettings = inferenceSettings.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Agent Coordinator starting...");
            
            try
            {
                // 预加载推理模型
                await PreloadModelsAsync();
                
                // 设置定时爬取任务
                await SetupScheduledCrawlsAsync();
                
                // 主循环
                while (!stoppingToken.IsCancellationRequested)
                {
                    // 检查任务状态
                    await CheckJobStatusAsync();
                    
                    // 等待一段时间
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Agent Coordinator stopping...");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Agent Coordinator");
            }
        }

        private async Task PreloadModelsAsync()
        {
            _logger.LogInformation("Preloading inference models...");
            
            try
            {
                // 加载默认文本分类模型
                if (!string.IsNullOrEmpty(_inferenceSettings.DefaultTextClassificationModel))
                {
                    await _inferenceModelService.LoadModelAsync(_inferenceSettings.DefaultTextClassificationModel);
                }
                
                // 加载默认实体提取模型
                if (!string.IsNullOrEmpty(_inferenceSettings.DefaultEntityExtractionModel))
                {
                    await _inferenceModelService.LoadModelAsync(_inferenceSettings.DefaultEntityExtractionModel);
                }
                
                // 加载默认嵌入模型
                if (!string.IsNullOrEmpty(_inferenceSettings.DefaultEmbeddingModel))
                {
                    await _inferenceModelService.LoadModelAsync(_inferenceSettings.DefaultEmbeddingModel);
                }
                
                _logger.LogInformation("Inference models preloaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preloading inference models");
            }
        }

        private async Task SetupScheduledCrawlsAsync()
        {
            _logger.LogInformation("Setting up scheduled crawls...");
            
            foreach (var crawl in _scheduledCrawls)
            {
                try
                {
                    var schedule = new CrawlSchedule
                    {
                        Name = crawl.Name,
                        CronExpression = crawl.CronExpression,
                        Sources = crawl.Sources,
                        MaxDepth = crawl.MaxDepth,
                        MaxPages = crawl.MaxPages
                    };
                    
                    await _crawlerManager.ScheduleCrawlJobAsync(schedule);
                    _logger.LogInformation($"Scheduled crawl {crawl.Name} with cron: {crawl.CronExpression}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error scheduling crawl {crawl.Name}");
                }
            }
        }

        private async Task CheckJobStatusAsync()
        {
            try
            {
                var activeJobs = await _crawlerManager.GetActiveCrawlJobsAsync();
                _logger.LogInformation($"Active crawl jobs: {activeJobs.Count()}");
                
                foreach (var job in activeJobs)
                {
                    _logger.LogDebug($"Job {job.JobId}: Status={job.Status}, LastRun={job.LastRunTime}, NextRun={job.NextRunTime}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking job status");
            }
        }
    }
} 