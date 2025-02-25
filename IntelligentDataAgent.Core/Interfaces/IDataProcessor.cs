using IntelligentDataAgent.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IntelligentDataAgent.Core.Interfaces
{
    public interface IDataProcessor
    {
        Task<List<Document>> ProcessDataAsync(IEnumerable<CrawledData> crawledData);
        Task<string> ExtractTextAsync(string html);
        Task<Dictionary<string, string>> ExtractMetadataAsync(string html);
    }
} 