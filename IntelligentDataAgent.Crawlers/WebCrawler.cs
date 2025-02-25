using IntelligentDataAgent.Core.Interfaces;
using IntelligentDataAgent.Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace IntelligentDataAgent.Crawlers
{
    public class WebCrawler : IWebCrawler
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<WebCrawler> _logger;
        private readonly ConcurrentDictionary<string, bool> _visitedUrls = new();
        private readonly ConcurrentBag<CrawledData> _crawledData = new();

        public WebCrawler(HttpClient httpClient, ILogger<WebCrawler> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<IEnumerable<CrawledData>> CrawlAsync(CrawlRequest request)
        {
            _logger.LogInformation($"Starting web crawl for request {request.Id}");
            
            _visitedUrls.Clear();
            _crawledData.Clear();
            
            var tasks = new List<Task>();
            
            foreach (var url in request.Sources)
            {
                tasks.Add(CrawlUrlAsync(url, 0, request.MaxDepth, request.MaxPages));
            }
            
            await Task.WhenAll(tasks);
            
            _logger.LogInformation($"Completed web crawl for request {request.Id}. Crawled {_crawledData.Count} pages");
            
            return _crawledData.ToList();
        }

        private async Task CrawlUrlAsync(string url, int currentDepth, int maxDepth, int maxPages)
        {
            // 检查是否已达到最大页面数
            if (_crawledData.Count >= maxPages)
                return;
            
            // 检查URL是否已访问
            if (_visitedUrls.ContainsKey(url))
                return;
            
            // 标记URL为已访问
            _visitedUrls[url] = true;
            
            try
            {
                _logger.LogDebug($"Crawling URL: {url}");
                
                // 发送HTTP请求
                var response = await _httpClient.GetAsync(url);
                
                // 检查响应状态
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Failed to crawl URL {url}: {response.StatusCode}");
                    return;
                }
                
                // 获取内容类型
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "text/html";
                
                // 读取内容
                var content = await response.Content.ReadAsStringAsync();
                
                // 创建爬取数据对象
                var crawledData = new CrawledData
                {
                    Id = Guid.NewGuid().ToString(),
                    Url = url,
                    Content = content,
                    ContentType = contentType,
                    Metadata = new Dictionary<string, string>(),
                    CrawlTime = DateTime.UtcNow
                };
                
                // 添加到结果集
                _crawledData.Add(crawledData);
                
                // 如果是HTML内容且未达到最大深度，则提取链接并继续爬取
                if (contentType.Contains("html") && currentDepth < maxDepth)
                {
                    var links = ExtractLinks(content, url);
                    
                    var tasks = new List<Task>();
                    
                    foreach (var link in links)
                    {
                        tasks.Add(CrawlUrlAsync(link, currentDepth + 1, maxDepth, maxPages));
                    }
                    
                    await Task.WhenAll(tasks);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error crawling URL {url}");
            }
        }

        private List<string> ExtractLinks(string html, string baseUrl)
        {
            var links = new List<string>();
            
            try
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(html);
                
                var linkNodes = doc.DocumentNode.SelectNodes("//a[@href]");
                
                if (linkNodes != null)
                {
                    foreach (var linkNode in linkNodes)
                    {
                        var href = linkNode.GetAttributeValue("href", "");
                        
                        if (string.IsNullOrEmpty(href) || href.StartsWith("#") || href.StartsWith("javascript:"))
                            continue;
                        
                        // 构建完整URL
                        Uri uri;
                        if (Uri.TryCreate(new Uri(baseUrl), href, out uri))
                        {
                            links.Add(uri.AbsoluteUri);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting links from {baseUrl}");
            }
            
            return links;
        }
    }
} 