using IntelligentDataAgent.Core.Interfaces;
using IntelligentDataAgent.Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IntelligentDataAgent.Crawlers
{
    public class CrawlerManager : ICrawlerManager
    {
        private readonly IWebCrawler _webCrawler;
        private readonly IApiCrawler _apiCrawler;
        private readonly ILogger<CrawlerManager> _logger;
        private readonly ConcurrentDictionary<string, CrawlJobInfo> _activeJobs = new();

        public CrawlerManager(
            IWebCrawler webCrawler,
            IApiCrawler apiCrawler,
            ILogger<CrawlerManager> logger)
        {
            _webCrawler = webCrawler;
            _apiCrawler = apiCrawler;
            _logger = logger;
        }

        public async Task<IEnumerable<CrawledData>> CrawlDataAsync(CrawlRequest request)
        {
            _logger.LogInformation($"Starting crawl for request {request.Id}");
            
            var results = new List<CrawledData>();
            
            // 根据URL类型选择合适的爬虫
            if (request.CrawlType == CrawlType.Web)
            {
                var webResults = await _webCrawler.CrawlAsync(request);
                results.AddRange(webResults);
            }
            else if (request.CrawlType == CrawlType.Api)
            {
                var apiResults = await _apiCrawler.CrawlAsync(request);
                results.AddRange(apiResults);
            }
            else // 混合模式
            {
                var webResults = await _webCrawler.CrawlAsync(request);
                var apiResults = await _apiCrawler.CrawlAsync(request);
                
                results.AddRange(webResults);
                results.AddRange(apiResults);
            }
            
            _logger.LogInformation($"Completed crawl for request {request.Id}. Crawled {results.Count} items.");
            return results;
        }

        public async Task ScheduleCrawlJobAsync(CrawlSchedule schedule)
        {
            var jobId = Guid.NewGuid().ToString();
            _logger.LogInformation($"Scheduling crawl job {jobId} with schedule: {schedule.CronExpression}");
            
            var cts = new CancellationTokenSource();
            var jobInfo = new CrawlJobInfo
            {
                JobId = jobId,
                Schedule = schedule,
                CancellationTokenSource = cts,
                Status = CrawlJobStatus.Scheduled
            };
            
            _activeJobs[jobId] = jobInfo;
            
            // 启动定时任务
            var task = Task.Run(async () =>
            {
                try
                {
                    // 这里应该实现基于Cron表达式的调度逻辑
                    // 简化起见，我们只是模拟一个定时执行的任务
                    while (!cts.Token.IsCancellationRequested)
                    {
                        jobInfo.Status = CrawlJobStatus.Running;
                        _logger.LogInformation($"Executing scheduled crawl job {jobId}");
                        
                        var request = new CrawlRequest
                        {
                            Id = jobId,
                            Sources = schedule.Sources,
                            MaxDepth = schedule.MaxDepth,
                            MaxPages = schedule.MaxPages,
                            CrawlType = schedule.CrawlType
                        };
                        
                        await CrawlDataAsync(request);
                        
                        jobInfo.Status = CrawlJobStatus.Scheduled;
                        jobInfo.LastRunTime = DateTime.UtcNow;
                        
                        // 等待下一次执行
                        // 实际应用中应该根据Cron表达式计算下一次执行时间
                        await Task.Delay(TimeSpan.FromHours(1), cts.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation($"Crawl job {jobId} was cancelled");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error in crawl job {jobId}");
                    jobInfo.Status = CrawlJobStatus.Failed;
                    jobInfo.ErrorMessage = ex.Message;
                }
                finally
                {
                    if (cts.Token.IsCancellationRequested)
                    {
                        _activeJobs.TryRemove(jobId, out _);
                    }
                }
            }, cts.Token);
            
            await Task.CompletedTask;
        }

        public Task CancelCrawlJobAsync(string jobId)
        {
            if (_activeJobs.TryGetValue(jobId, out var jobInfo))
            {
                _logger.LogInformation($"Cancelling crawl job {jobId}");
                jobInfo.CancellationTokenSource.Cancel();
                return Task.CompletedTask;
            }
            
            _logger.LogWarning($"Attempted to cancel non-existent job {jobId}");
            throw new KeyNotFoundException($"Crawl job {jobId} not found");
        }

        public Task<IEnumerable<CrawlJobStatus>> GetActiveCrawlJobsAsync()
        {
            var jobStatuses = _activeJobs.Values.Select(j => new CrawlJobStatus
            {
                JobId = j.JobId,
                Status = j.Status.Status,
                LastRunTime = j.LastRunTime,
                NextRunTime = j.NextRunTime,
                ErrorMessage = j.ErrorMessage
            });
            
            return Task.FromResult<IEnumerable<CrawlJobStatus>>(jobStatuses.ToList());
        }

        private class CrawlJobInfo
        {
            public string JobId { get; set; }
            public CrawlSchedule Schedule { get; set; }
            public CancellationTokenSource CancellationTokenSource { get; set; }
            public CrawlJobStatus Status { get; set; }
            public DateTime? LastRunTime { get; set; }
            public DateTime? NextRunTime { get; set; }
            public string ErrorMessage { get; set; }
        }
    }
} 