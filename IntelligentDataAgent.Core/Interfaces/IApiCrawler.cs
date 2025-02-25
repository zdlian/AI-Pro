using IntelligentDataAgent.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IntelligentDataAgent.Core.Interfaces
{
    public interface IApiCrawler
    {
        Task<IEnumerable<CrawledData>> CrawlAsync(CrawlRequest request);
    }
} 