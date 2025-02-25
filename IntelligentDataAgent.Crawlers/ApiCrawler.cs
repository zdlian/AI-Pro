using IntelligentDataAgent.Core.Interfaces;
using IntelligentDataAgent.Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace IntelligentDataAgent.Crawlers
{
    public class ApiCrawler : IApiCrawler
    {
        private readonly ILogger<ApiCrawler> _logger;
        private readonly HttpClient _httpClient;

        public ApiCrawler(ILogger<ApiCrawler> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<CrawledData>> CrawlAsync(CrawlRequest request)
        {
            _logger.LogInformation($"Starting API crawl for request {request.Id}");
            
            var results = new List<CrawledData>();
            
            foreach (var apiUrl in request.Sources)
            {
                try
                {
                    var crawledData = await CrawlApiEndpointAsync(apiUrl, request.Headers, request.Parameters);
                    if (crawledData != null)
                    {
                        results.Add(crawledData);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error crawling API endpoint {apiUrl}");
                }
                
                if (results.Count >= request.MaxPages)
                {
                    break;
                }
            }
            
            _logger.LogInformation($"API crawl completed for request {request.Id}. Crawled {results.Count} endpoints.");
            return results;
        }

        private async Task<CrawledData> CrawlApiEndpointAsync(
            string apiUrl, 
            Dictionary<string, string> headers, 
            Dictionary<string, string> parameters)
        {
            _logger.LogDebug($"Crawling API endpoint: {apiUrl}");
            
            try
            {
                // 准备请求
                var request = new HttpRequestMessage();
                
                // 设置URL和查询参数
                var uriBuilder = new UriBuilder(apiUrl);
                if (parameters != null && parameters.Count > 0)
                {
                    var query = new StringBuilder(uriBuilder.Query.TrimStart('?'));
                    foreach (var param in parameters)
                    {
                        if (query.Length > 0)
                        {
                            query.Append('&');
                        }
                        query.Append($"{Uri.EscapeDataString(param.Key)}={Uri.EscapeDataString(param.Value)}");
                    }
                    uriBuilder.Query = query.ToString();
                }
                
                request.RequestUri = uriBuilder.Uri;
                
                // 设置请求方法（默认为GET）
                request.Method = HttpMethod.Get;
                
                // 设置请求头
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }
                
                // 发送请求
                var response = await _httpClient.SendAsync(request);
                
                // 处理响应
                var content = await response.Content.ReadAsStringAsync();
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/json";
                
                return new CrawledData
                {
                    Id = Guid.NewGuid().ToString(),
                    Url = apiUrl,
                    Content = content,
                    ContentType = contentType,
                    CrawlTime = DateTime.UtcNow,
                    Metadata = new Dictionary<string, string>
                    {
                        { "StatusCode", ((int)response.StatusCode).ToString() },
                        { "ContentLength", response.Content.Headers.ContentLength?.ToString() ?? "0" }
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error crawling API endpoint {apiUrl}");
                return null;
            }
        }
    }
} 