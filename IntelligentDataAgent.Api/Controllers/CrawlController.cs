using IntelligentDataAgent.Core.Interfaces;
using IntelligentDataAgent.Core.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace IntelligentDataAgent.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CrawlController : ControllerBase
    {
        private readonly ICrawlerManager _crawlerManager;
        private readonly IDataProcessor _dataProcessor;
        private readonly IElasticsearchService _elasticsearchService;
        private readonly ILogger<CrawlController> _logger;

        public CrawlController(
            ICrawlerManager crawlerManager,
            IDataProcessor dataProcessor,
            IElasticsearchService elasticsearchService,
            ILogger<CrawlController> logger)
        {
            _crawlerManager = crawlerManager;
            _dataProcessor = dataProcessor;
            _elasticsearchService = elasticsearchService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> StartCrawl([FromBody] CrawlRequest request)
        {
            try
            {
                _logger.LogInformation($"Starting crawl with {request.Sources.Count} sources");
                
                // 执行爬取
                var crawledData = await _crawlerManager.CrawlDataAsync(request);
                
                // 处理数据
                var documents = await _dataProcessor.ProcessDataAsync(crawledData);
                
                // 索引数据
                await _elasticsearchService.IndexDocumentsAsync("intelligent_agent_index", documents);
                
                return Ok(new
                {
                    Success = true,
                    CrawledCount = crawledData.Count(),
                    ProcessedCount = documents.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during crawl");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpPost("schedule")]
        public async Task<IActionResult> ScheduleCrawl([FromBody] CrawlSchedule schedule)
        {
            try
            {
                await _crawlerManager.ScheduleCrawlJobAsync(schedule);
                return Ok(new { Success = true, Message = $"Scheduled crawl job: {schedule.Name}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling crawl");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpDelete("{jobId}")]
        public async Task<IActionResult> CancelCrawl(string jobId)
        {
            try
            {
                await _crawlerManager.CancelCrawlJobAsync(jobId);
                return Ok(new { Success = true, Message = $"Cancelled crawl job: {jobId}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling crawl");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpGet("jobs")]
        public async Task<IActionResult> GetActiveJobs()
        {
            try
            {
                var jobs = await _crawlerManager.GetActiveCrawlJobsAsync();
                return Ok(jobs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active jobs");
                return StatusCode(500, new { Error = ex.Message });
            }
        }
    }
} 