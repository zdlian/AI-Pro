using IntelligentDataAgent.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IntelligentDataAgent.Core.Interfaces
{
    public interface IElasticsearchService
    {
        Task CreateIndexIfNotExistsAsync(string indexName);
        Task IndexDocumentsAsync<T>(string indexName, IEnumerable<T> documents) where T : class;
        Task<SearchResult<T>> SearchAsync<T>(SearchRequest request) where T : class;
        Task DeleteIndexAsync(string indexName);
        Task<bool> DeleteDocumentAsync(string indexName, string id);
        Task<bool> UpdateDocumentAsync<T>(string indexName, string id, T document) where T : class;
        Task<T> GetDocumentAsync<T>(string indexName, string id) where T : class;
    }
} 