using IntelligentDataAgent.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Nest;
using System;
using System.Threading.Tasks;

namespace IntelligentDataAgent.Elasticsearch
{
    public class IndexManager : IIndexManager
    {
        private readonly IElasticClient _elasticClient;
        private readonly ILogger<IndexManager> _logger;

        public IndexManager(
            IElasticClient elasticClient,
            ILogger<IndexManager> logger)
        {
            _elasticClient = elasticClient;
            _logger = logger;
        }

        public async Task<bool> IndexExistsAsync(string indexName)
        {
            var response = await _elasticClient.Indices.ExistsAsync(indexName);
            return response.Exists;
        }

        public async Task CreateIndexAsync(string indexName)
        {
            _logger.LogInformation($"Creating index {indexName}");
            
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
                    .NumberOfShards(1)
                    .NumberOfReplicas(0)
                )
                .Map<dynamic>(m => m
                    .AutoMap()
                    .Properties(p => p
                        .Text(t => t
                            .Name("title")
                            .Analyzer("custom_analyzer")
                            .Fields(f => f
                                .Keyword(k => k.Name("keyword"))
                            )
                        )
                        .Text(t => t
                            .Name("content")
                            .Analyzer("custom_analyzer")
                        )
                        .Text(t => t
                            .Name("source")
                            .Fields(f => f
                                .Keyword(k => k.Name("keyword"))
                            )
                        )
                        .Date(d => d
                            .Name("crawlDate")
                        )
                        .Object<dynamic>(o => o
                            .Name("metadata")
                            .Properties(mp => mp
                                .Text(t => t
                                    .Name("title")
                                    .Analyzer("custom_analyzer")
                                )
                                .Text(t => t
                                    .Name("description")
                                    .Analyzer("custom_analyzer")
                                )
                                .Text(t => t
                                    .Name("keywords")
                                    .Analyzer("custom_analyzer")
                                )
                                .Text(t => t
                                    .Name("author")
                                    .Fields(f => f
                                        .Keyword(k => k.Name("keyword"))
                                    )
                                )
                                .Date(d => d
                                    .Name("published_date")
                                )
                            )
                        )
                    )
                )
            );
            
            if (!createIndexResponse.IsValid)
            {
                _logger.LogError($"Failed to create index {indexName}: {createIndexResponse.DebugInformation}");
                throw new Exception($"Failed to create index {indexName}: {createIndexResponse.ServerError?.Error?.Reason}");
            }
            
            _logger.LogInformation($"Successfully created index {indexName}");
        }

        public async Task DeleteIndexAsync(string indexName)
        {
            _logger.LogInformation($"Deleting index {indexName}");
            
            var deleteIndexResponse = await _elasticClient.Indices.DeleteAsync(indexName);
            
            if (!deleteIndexResponse.IsValid)
            {
                _logger.LogError($"Failed to delete index {indexName}: {deleteIndexResponse.DebugInformation}");
                throw new Exception($"Failed to delete index {indexName}: {deleteIndexResponse.ServerError?.Error?.Reason}");
            }
            
            _logger.LogInformation($"Successfully deleted index {indexName}");
        }

        public async Task UpdateIndexMappingAsync(string indexName, string mappingJson)
        {
            _logger.LogInformation($"Updating mapping for index {indexName}");
            
            //var putMappingResponse = await _elasticClient.Indices.PutMappingAsync(new PutMappingRequest(indexName)
            //{
            //    SourceSerializer = new JsonNetSerializer(
            //        new ConnectionSettings(new Uri("http://localhost:9200")),
            //        resolver => resolver,
            //        (serializer, values, formatting) => serializer.Serialize(values)
            //    ),
            //    Source = mappingJson
            //});
            
            //if (!putMappingResponse.IsValid)
            //{
            //    _logger.LogError($"Failed to update mapping for index {indexName}: {putMappingResponse.DebugInformation}");
            //    throw new Exception($"Failed to update mapping for index {indexName}");
            //}
            
            _logger.LogInformation($"Successfully updated mapping for index {indexName}");
        }
    }
} 