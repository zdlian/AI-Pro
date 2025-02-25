using IntelligentDataAgent.Core.Interfaces;
using IntelligentDataAgent.Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IntelligentDataAgent.Processing
{
    public class DataProcessor : IDataProcessor
    {
        private readonly ITextExtractor _textExtractor;
        private readonly IDataNormalizer _dataNormalizer;
        private readonly ILogger<DataProcessor> _logger;

        public DataProcessor(
            ITextExtractor textExtractor,
            IDataNormalizer dataNormalizer,
            ILogger<DataProcessor> logger)
        {
            _textExtractor = textExtractor;
            _dataNormalizer = dataNormalizer;
            _logger = logger;
        }

        public async Task<List<Document>> ProcessDataAsync(IEnumerable<CrawledData> crawledData)
        {
            _logger.LogInformation($"Processing {crawledData.Count()} crawled items");
            
            var documents = new List<Document>();
            
            foreach (var data in crawledData)
            {
                try
                {
                    // 提取文本内容
                    string extractedText;
                    if (data.ContentType.Contains("html", StringComparison.OrdinalIgnoreCase))
                    {
                        extractedText = await ExtractTextAsync(data.Content);
                    }
                    else
                    {
                        extractedText = data.Content;
                    }
                    
                    // 提取元数据
                    var metadata = await ExtractMetadataAsync(data.Content);
                    
                    // 规范化数据
                    var normalizedText = _dataNormalizer.NormalizeText(extractedText);
                    
                    // 创建文档
                    var document = new Document
                    {
                        Id = Guid.NewGuid().ToString(),
                        Title = metadata.ContainsKey("title") ? metadata["title"] : data.Url,
                        Content = normalizedText,
                        Source = data.Url,
                        CrawlDate = data.CrawlTime,
                        Metadata = metadata
                    };
                    
                    documents.Add(document);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing crawled data from {data.Url}");
                }
            }
            
            _logger.LogInformation($"Successfully processed {documents.Count} documents");
            return documents;
        }

        public async Task<string> ExtractTextAsync(string html)
        {
            return await _textExtractor.ExtractTextFromHtmlAsync(html);
        }

        public async Task<Dictionary<string, string>> ExtractMetadataAsync(string html)
        {
            return await _textExtractor.ExtractMetadataFromHtmlAsync(html);
        }
    }
} 