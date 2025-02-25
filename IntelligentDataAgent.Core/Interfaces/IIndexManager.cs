using System.Threading.Tasks;

namespace IntelligentDataAgent.Core.Interfaces
{
    public interface IIndexManager
    {
        Task<bool> IndexExistsAsync(string indexName);
        Task CreateIndexAsync(string indexName);
        Task DeleteIndexAsync(string indexName);
        Task UpdateIndexMappingAsync(string indexName, string mappingJson);
    }
} 