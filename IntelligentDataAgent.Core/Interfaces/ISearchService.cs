using IntelligentDataAgent.Core.Models;
using System.Threading.Tasks;

namespace IntelligentDataAgent.Core.Interfaces
{
    public interface ISearchService
    {
        Task<SearchResult<T>> SearchAsync<T>(SearchRequest request) where T : class;
        Task<T> GetDocumentByIdAsync<T>(string indexName, string id) where T : class;
        Task<long> CountDocumentsAsync(string indexName, string query = null);
    }
} 