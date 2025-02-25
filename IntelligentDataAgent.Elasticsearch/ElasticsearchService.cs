using IntelligentDataAgent.Core.Interfaces;
using IntelligentDataAgent.Core.Models;
using Microsoft.Extensions.Logging;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SearchRequest = IntelligentDataAgent.Core.Models.SearchRequest;

namespace IntelligentDataAgent.Elasticsearch
{
    public class ElasticsearchService : IElasticsearchService
    {
        private readonly IElasticClient _elasticClient;
        private readonly IIndexManager _indexManager;
        private readonly ILogger<ElasticsearchService> _logger;

        public ElasticsearchService(
            IElasticClient elasticClient,
            IIndexManager indexManager,
            ILogger<ElasticsearchService> logger)
        {
            _elasticClient = elasticClient;
            _indexManager = indexManager;
            _logger = logger;
        }

        public async Task CreateIndexIfNotExistsAsync(string indexName)
        {
            var indexExists = await _elasticClient.Indices.ExistsAsync(indexName);
            
            if (!indexExists.Exists)
            {
                var createIndexResponse = await _elasticClient.Indices.CreateAsync(indexName, c => c
                    .Settings(s => s
                        .Analysis(a => a
                            .Analyzers(an => an
                                .Custom("custom_analyzer", ca => ca
                                    .Tokenizer("standard")
                                    .Filters("lowercase", "asciifolding")
                                )
                            )
                        )
                        .NumberOfShards(3)
                        .NumberOfReplicas(1)
                    )
                    .Map<Document>(m => m
                        .AutoMap()
                        .Properties(p => p
                            .Text(t => t
                                .Name(n => n.Content)
                                .Analyzer("custom_analyzer")
                            )
                            .Keyword(k => k
                                .Name(n => n.Source)
                            )
                            .Date(d => d
                                .Name(n => n.CrawlDate)
                            )
                        )
                    )
                );

                if (!createIndexResponse.IsValid)
                {
                    _logger.LogError($"Failed to create index {indexName}: {createIndexResponse.DebugInformation}");
                    throw new Exception($"Failed to create index {indexName}");
                }
                
                _logger.LogInformation($"Created index {indexName}");
            }
        }

        public async Task IndexDocumentsAsync<T>(string indexName, IEnumerable<T> documents) where T : class
        {
            _logger.LogInformation($"Indexing {documents.Count()} documents to index {indexName}");
            
            // 确保索引存在
            var indexExists = await _indexManager.IndexExistsAsync(indexName);
            if (!indexExists)
            {
                await _indexManager.CreateIndexAsync(indexName);
            }
            
            // 批量索引文档
            var bulkRequest = new BulkRequest(indexName)
            {
                Operations = new List<IBulkOperation>()
            };
            
            foreach (var document in documents)
            {
                bulkRequest.Operations.Add(new BulkIndexOperation<T>(document));
            }
            
            var response = await _elasticClient.BulkAsync(bulkRequest);
            
            if (response.Errors)
            {
                var failedItems = response.ItemsWithErrors.Select(i => i.Error).ToList();
                _logger.LogError($"Failed to index some documents: {string.Join(", ", failedItems)}");
                throw new Exception($"Failed to index {failedItems.Count} documents");
            }
            
            _logger.LogInformation($"Successfully indexed {documents.Count()} documents to index {indexName}");
        }

        public async Task<SearchResult<T>> SearchAsync<T>(SearchRequest request) where T : class
        {
            var searchResponse = await _elasticClient.SearchAsync<T>(s => s
                .Index(request.IndexName)
                .From(request.From)
                .Size(request.Size)
                .Query(q => q
                    .Bool(b => b
                        .Must(m => m
                            .QueryString(qs => qs
                                .Query(request.Query)
                            )
                        )
                        //.Filter(BuildFilters(request.Filters))
                    )
                )
                //.Sort(BuildSortCriteria(request.SortFields))
            );

            if (!searchResponse.IsValid)
            {
                _logger.LogError($"Search failed: {searchResponse.DebugInformation}");
                throw new Exception("Search failed");
            }

            return new SearchResult<T>
            {
                Total = searchResponse.Total,
                //Documents = searchResponse.Documents,
                Took = searchResponse.Took,
                TimedOut = searchResponse.TimedOut
            };
        }

        public async Task DeleteIndexAsync(string indexName)
        {
            var response = await _elasticClient.Indices.DeleteAsync(indexName);
            
            if (!response.IsValid)
            {
                _logger.LogError($"Failed to delete index {indexName}: {response.DebugInformation}");
                throw new Exception($"Failed to delete index {indexName}");
            }
            
            _logger.LogInformation($"Successfully deleted index {indexName}");
        }

        private Func<SortDescriptor<T>, IPromise<IList<ISort>>> BuildSortCriteria<T>(
            IEnumerable<SortField> sortFields) where T : class
        {
            return s =>
            {
                var descriptor = new SortDescriptor<T>();
                
                foreach (var sortField in sortFields ?? Enumerable.Empty<SortField>())
                {
                    descriptor = descriptor.Field(f => f
                        .Field(sortField.FieldName)
                        .Order(sortField.Ascending ? SortOrder.Ascending : SortOrder.Descending)
                    );
                }
                
                return descriptor;
            };
        }

        private Func<QueryContainerDescriptor<T>, QueryContainer>[] BuildFilters<T>(
            IEnumerable<Core.Models.Filter> filters) where T : class
        {
            var filterFuncs = new List<Func<QueryContainerDescriptor<T>, QueryContainer>>();
            
            foreach (var filter in filters ?? Enumerable.Empty<Core.Models.Filter>())
            {
                switch (filter.Type)
                {
                    case FilterType.Term:
                        filterFuncs.Add(f => f
                            .Term(t => t
                                .Field(filter.Field)
                                .Value(filter.Value)
                            )
                        );
                        break;
                    case FilterType.Range:
                        var rangeFilter = filter as RangeFilter;
                        filterFuncs.Add(f => f
                            .Range(r => r
                                .Field(filter.Field)
                                .GreaterThanOrEquals(rangeFilter?.From)
                                .LessThanOrEquals(rangeFilter?.To)
                            )
                        );
                        break;
                    // 可以添加更多过滤器类型
                }
            }
            
            return filterFuncs.ToArray();
        }

        public async Task<bool> DeleteDocumentAsync(string indexName, string id)
        {
            _logger.LogInformation($"Deleting document {id} from index {indexName}");
            
            var response = await _elasticClient.DeleteAsync(new DeleteRequest(indexName, id));
            
            if (!response.IsValid)
            {
                _logger.LogError($"Failed to delete document {id}: {response.DebugInformation}");
                return false;
            }
            
            return true;
        }

        public async Task<bool> UpdateDocumentAsync<T>(string indexName, string id, T document) where T : class
        {
            _logger.LogInformation($"Updating document {id} in index {indexName}");

            //var response = await _elasticClient.UpdateAsync<T, object>(new UpdateRequest<T, object>(indexName, id)
            //{
            //    Doc = document
            //}, selector => selector);

            //if (!response.IsValid)
            //{
            //    _logger.LogError($"Failed to update document {id}: {response.DebugInformation}");
            //    return false;
            //}

            return true;
        }

        public async Task<T> GetDocumentAsync<T>(string indexName, string id) where T : class
        {
            _logger.LogInformation($"Getting document {id} from index {indexName}");
            
            var response = await _elasticClient.GetAsync<T>(new GetRequest(indexName, id));
            
            if (!response.IsValid)
            {
                _logger.LogError($"Failed to get document {id}: {response.DebugInformation}");
                return null;
            }
            
            return response.Source;
        }
    }
} 