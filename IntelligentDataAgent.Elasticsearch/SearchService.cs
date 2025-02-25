using IntelligentDataAgent.Core.Interfaces;
using IntelligentDataAgent.Core.Models;
using Microsoft.Extensions.Logging;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Filter = IntelligentDataAgent.Core.Models.Filter;

namespace IntelligentDataAgent.Elasticsearch
{
    public class SearchService : ISearchService
    {
        private readonly IElasticClient _elasticClient;
        private readonly ILogger<SearchService> _logger;

        public SearchService(
            IElasticClient elasticClient,
            ILogger<SearchService> logger)
        {
            _elasticClient = elasticClient;
            _logger = logger;
        }

        public async Task<SearchResult<T>> SearchAsync<T>(Core.Models.SearchRequest request) where T : class
        {
            _logger.LogInformation($"Searching in index {request.IndexName} with query: {request.Query}");
            
            try
            {
                var searchDescriptor = new SearchDescriptor<T>()
                    .Index(request.IndexName)
                    .From(request.From)
                    .Size(request.Size);
                
                // 添加查询
                if (!string.IsNullOrEmpty(request.Query))
                {
                    searchDescriptor = searchDescriptor.Query(q => q
                        .MultiMatch(mm => mm
                            .Fields(f => f
                                .Field("*")
                            )
                            .Query(request.Query)
                            .Type(TextQueryType.BestFields)
                            .Fuzziness(Fuzziness.Auto)
                        )
                    );
                }
                else
                {
                    searchDescriptor = searchDescriptor.Query(q => q.MatchAll());
                }
                
                // 添加过滤器
                if (request.Filters != null && request.Filters.Count > 0)
                {
                    /*searchDescriptor = searchDescriptor.PostFilter(f => BuildFilterContainer(f, request.Filters));*/
                }

                // 添加排序
                /*if (!string.IsNullOrEmpty(request.SortField))
                {
                    *//*searchDescriptor = searchDescriptor.Sort(s => 
                        request.SortAscending 
                            ? s.Ascending(request.SortField) 
                            : s.Descending(request.SortField)
                    );*//*
                }*/

                // 添加聚合
                searchDescriptor = searchDescriptor.Aggregations(a => 
                    a.Terms("source", t => t.Field("source.keyword").Size(10))
                     .Terms("contentType", t => t.Field("contentType.keyword").Size(10))
                     .DateHistogram("crawlDate", d => d.Field("crawlDate").CalendarInterval(DateInterval.Month))
                );
                
                var response = await _elasticClient.SearchAsync<T>(searchDescriptor);
                
                if (!response.IsValid)
                {
                    _logger.LogError($"Search failed: {response.DebugInformation}");
                    throw new Exception($"Search failed: {response.ServerError?.Error?.Reason}");
                }
                
                // 构建结果
                var result = new SearchResult<T>
                {
                    TotalHits = response.Total,
                    Documents = response.Documents.ToList()
                };
                
                // 处理聚合结果
                if (response.Aggregations != null)
                {
                    foreach (var agg in response.Aggregations)
                    {
                        if (agg.Value is BucketAggregate bucketAgg)
                        {
                            var facets = new List<Facet>();
                            
                            foreach (var bucket in bucketAgg.Items)
                            {
                                if (bucket is KeyedBucket<object> keyedBucket)
                                {
                                    facets.Add(new Facet
                                    {
                                        Value = keyedBucket.Key.ToString(),
                                        Count = keyedBucket.DocCount ?? 0
                                    });
                                }
                            }
                            
                            result.Facets[agg.Key] = facets;
                        }
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during search");
                throw;
            }
        }

        public async Task<T> GetDocumentByIdAsync<T>(string indexName, string id) where T : class
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

        public async Task<long> CountDocumentsAsync(string indexName, string query = null)
        {
            _logger.LogInformation($"Counting documents in index {indexName}");
            
            var countRequest = new CountRequest(indexName);
            
            if (!string.IsNullOrEmpty(query))
            {
                countRequest.Query = new QueryContainer(
                    new MultiMatchQuery
                    {
                        Fields = "*",
                        Query = query,
                        Type = TextQueryType.BestFields,
                        Fuzziness = Fuzziness.Auto
                    }
                );
            }
            
            var response = await _elasticClient.CountAsync(countRequest);
            
            if (!response.IsValid)
            {
                _logger.LogError($"Failed to count documents: {response.DebugInformation}");
                throw new Exception($"Count failed: {response.ServerError?.Error?.Reason}");
            }
            
            return response.Count;
        }

      /*  private QueryContainer BuildFilterContainer(QueryContainerDescriptor<T> descriptor, List<Filter> filters)
        {
            var filterContainers = new List<QueryContainer>();
            
            foreach (var filter in filters)
            {
                *//*if (filter is TermFilter termFilter)
                {
                    filterContainers.Add(descriptor.Term(termFilter.Field, termFilter.Value));
                }
                else if (filter is RangeFilter rangeFilter)
                {
                    filterContainers.Add(descriptor.Range(r => r
                        .Field(rangeFilter.Field)
                        .GreaterThanOrEquals(rangeFilter.From)
                        .LessThanOrEquals(rangeFilter.To)
                    ));
                }*//*
            }
            
            return descriptor.Bool(b => b.Filter(filterContainers));
        }*/
    }
} 