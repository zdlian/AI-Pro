using IntelligentDataAgent.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IntelligentDataAgent.Core.Interfaces
{
    public interface ICrawlerManager
    {
        Task<IEnumerable<CrawledData>> CrawlDataAsync(CrawlRequest request);
        Task ScheduleCrawlJobAsync(CrawlSchedule schedule);
        Task CancelCrawlJobAsync(string jobId);
        Task<IEnumerable<CrawlJobStatus>> GetActiveCrawlJobsAsync();
    }
} 
 