using HtmlAgilityPack;
using IntelligentDataAgent.Core.Interfaces;
using IntelligentDataAgent.Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IntelligentDataAgent.Processing
{
    public class TextExtractor : ITextExtractor
    {
        private readonly ILogger<TextExtractor> _logger;

        public TextExtractor(ILogger<TextExtractor> logger)
        {
            _logger = logger;
        }

        public Task<ExtractionResult> ExtractTextAsync(CrawledData data)
        {
            _logger.LogDebug($"Extracting text from {data.Url}");
            
            try
            {
                if (string.IsNullOrEmpty(data.Content))
                {
                    return Task.FromResult(new ExtractionResult
                    {
                        Success = false,
                        ErrorMessage = "Content is empty"
                    });
                }
                
                // 根据内容类型选择不同的提取方法
                switch (data.ContentType?.ToLowerInvariant())
                {
                    case "text/html":
                        return Task.FromResult(ExtractFromHtml(data.Content));
                    case "application/json":
                        return Task.FromResult(ExtractFromJson(data.Content));
                    case "text/plain":
                        return Task.FromResult(ExtractFromPlainText(data.Content));
                    case "application/pdf":
                        return Task.FromResult(ExtractFromPdf(data.Content));
                    default:
                        // 默认尝试作为HTML处理
                        return Task.FromResult(ExtractFromHtml(data.Content));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting text from {data.Url}");
                return Task.FromResult(new ExtractionResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        private ExtractionResult ExtractFromHtml(string htmlContent)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);
            
            // 移除脚本、样式和注释节点
            var nodesToRemove = doc.DocumentNode.SelectNodes("//script|//style|//comment()");
            if (nodesToRemove != null)
            {
                foreach (var node in nodesToRemove)
                {
                    node.Remove();
                }
            }
            
            // 提取标题
            var title = doc.DocumentNode.SelectSingleNode("//title")?.InnerText.Trim();
            
            // 提取正文内容
            var contentBuilder = new StringBuilder();
            
            // 优先提取文章主体内容
            var mainContent = doc.DocumentNode.SelectSingleNode("//article|//main|//div[@id='content']|//div[@class='content']");
            if (mainContent != null)
            {
                ExtractTextFromNode(mainContent, contentBuilder);
            }
            else
            {
                // 如果没有明确的主体内容，则提取所有段落和标题
                var paragraphs = doc.DocumentNode.SelectNodes("//p|//h1|//h2|//h3|//h4|//h5|//h6");
                if (paragraphs != null)
                {
                    foreach (var paragraph in paragraphs)
                    {
                        ExtractTextFromNode(paragraph, contentBuilder);
                    }
                }
                else
                {
                    // 如果没有段落和标题，则提取body中的所有文本
                    var body = doc.DocumentNode.SelectSingleNode("//body");
                    if (body != null)
                    {
                        ExtractTextFromNode(body, contentBuilder);
                    }
                }
            }
            
            // 检测语言（实际实现中可能会使用语言检测库）
            var detectedLanguage = DetectLanguage(contentBuilder.ToString());
            
            return new ExtractionResult
            {
                Success = true,
                Title = title,
                Text = contentBuilder.ToString(),
                DetectedLanguage = detectedLanguage
            };
        }

        private void ExtractTextFromNode(HtmlNode node, StringBuilder contentBuilder)
        {
            if (node.NodeType == HtmlNodeType.Text)
            {
                var text = node.InnerText.Trim();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    contentBuilder.AppendLine(text);
                }
            }
            else
            {
                foreach (var childNode in node.ChildNodes)
                {
                    ExtractTextFromNode(childNode, contentBuilder);
                }
                
                // 对于块级元素，添加额外的换行
                if (IsBlockElement(node.Name))
                {
                    contentBuilder.AppendLine();
                }
            }
        }

        private bool IsBlockElement(string tagName)
        {
            var blockElements = new[] { "div", "p", "h1", "h2", "h3", "h4", "h5", "h6", "ul", "ol", "li", "table", "tr", "section", "article", "header", "footer" };
            return blockElements.Contains(tagName.ToLowerInvariant());
        }

        private ExtractionResult ExtractFromJson(string jsonContent)
        {
            // 简单实现，实际中可能需要更复杂的JSON解析
            return new ExtractionResult
            {
                Success = true,
                Text = jsonContent,
                DetectedLanguage = "unknown"
            };
        }

        private ExtractionResult ExtractFromPlainText(string textContent)
        {
            var detectedLanguage = DetectLanguage(textContent);
            
            return new ExtractionResult
            {
                Success = true,
                Text = textContent,
                DetectedLanguage = detectedLanguage
            };
        }

        private ExtractionResult ExtractFromPdf(string pdfContent)
        {
            // 实际实现中，这里会使用PDF解析库
            // 这里只是一个占位符
            return new ExtractionResult
            {
                Success = false,
                ErrorMessage = "PDF extraction not implemented"
            };
        }

        private string DetectLanguage(string text)
        {
            // 简单的语言检测实现
            // 实际实现中，可能会使用专门的语言检测库
            
            // 检查是否包含大量中文字符
            var chineseCharCount = Regex.Matches(text, @"[\u4e00-\u9fa5]").Count;
            if (chineseCharCount > text.Length * 0.1)
            {
                return "zh";
            }
            
            // 检查是否包含大量日文字符
            var japaneseCharCount = Regex.Matches(text, @"[\u3040-\u309F\u30A0-\u30FF]").Count;
            if (japaneseCharCount > text.Length * 0.1)
            {
                return "ja";
            }
            
            // 检查是否包含大量韩文字符
            var koreanCharCount = Regex.Matches(text, @"[\uAC00-\uD7A3]").Count;
            if (koreanCharCount > text.Length * 0.1)
            {
                return "ko";
            }
            
            // 默认假设为英文
            return "en";
        }

        public Task<string> ExtractTextFromHtmlAsync(string html)
        {
            try
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(html);
                
                // 移除脚本、样式和其他不需要的元素
                var nodesToRemove = new List<HtmlNode>();
                var nodesWithScripts = doc.DocumentNode.SelectNodes("//script|//style|//noscript|//iframe|//svg");
                if (nodesWithScripts != null)
                {
                    nodesToRemove.AddRange(nodesWithScripts);
                }
                
                foreach (var node in nodesToRemove)
                {
                    node.Remove();
                }
                
                // 提取文本内容
                var sb = new StringBuilder();
                
                // 首先尝试获取主要内容区域
                var mainContent = doc.DocumentNode.SelectNodes("//article|//main|//div[@id='content']|//div[@class='content']");
                if (mainContent != null && mainContent.Count > 0)
                {
                    foreach (var node in mainContent)
                    {
                        sb.AppendLine(CleanText(node.InnerText));
                    }
                }
                else
                {
                    // 如果没有找到主要内容区域，则提取所有段落和标题
                    var contentNodes = doc.DocumentNode.SelectNodes("//p|//h1|//h2|//h3|//h4|//h5|//h6");
                    if (contentNodes != null)
                    {
                        foreach (var node in contentNodes)
                        {
                            var text = CleanText(node.InnerText);
                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                sb.AppendLine(text);
                            }
                        }
                    }
                    else
                    {
                        // 如果没有找到段落和标题，则提取所有文本
                        sb.AppendLine(CleanText(doc.DocumentNode.InnerText));
                    }
                }
                
                return Task.FromResult(sb.ToString().Trim());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from HTML");
                return Task.FromResult(string.Empty);
            }
        }

        public Task<Dictionary<string, string>> ExtractMetadataFromHtmlAsync(string html)
        {
            var metadata = new Dictionary<string, string>();
            
            try
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(html);
                
                // 提取标题
                var titleNode = doc.DocumentNode.SelectSingleNode("//title");
                if (titleNode != null)
                {
                    metadata["title"] = CleanText(titleNode.InnerText);
                }
                
                // 提取元描述
                var metaDescription = doc.DocumentNode.SelectSingleNode("//meta[@name='description']");
                if (metaDescription != null)
                {
                    var content = metaDescription.GetAttributeValue("content", "");
                    if (!string.IsNullOrEmpty(content))
                    {
                        metadata["description"] = CleanText(content);
                    }
                }
                
                // 提取关键词
                var metaKeywords = doc.DocumentNode.SelectSingleNode("//meta[@name='keywords']");
                if (metaKeywords != null)
                {
                    var content = metaKeywords.GetAttributeValue("content", "");
                    if (!string.IsNullOrEmpty(content))
                    {
                        metadata["keywords"] = CleanText(content);
                    }
                }
                
                // 提取作者
                var metaAuthor = doc.DocumentNode.SelectSingleNode("//meta[@name='author']");
                if (metaAuthor != null)
                {
                    var content = metaAuthor.GetAttributeValue("content", "");
                    if (!string.IsNullOrEmpty(content))
                    {
                        metadata["author"] = CleanText(content);
                    }
                }
                
                // 提取发布日期
                var metaDate = doc.DocumentNode.SelectSingleNode("//meta[@property='article:published_time']");
                if (metaDate != null)
                {
                    var content = metaDate.GetAttributeValue("content", "");
                    if (!string.IsNullOrEmpty(content))
                    {
                        metadata["published_date"] = content;
                    }
                }
                
                return Task.FromResult(metadata);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting metadata from HTML");
                return Task.FromResult(metadata);
            }
        }

        private string CleanText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;
            
            // 替换多个空白字符为单个空格
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
            
            // 替换HTML实体
            text = System.Net.WebUtility.HtmlDecode(text);
            
            // 移除前后空白
            text = text.Trim();
            
            return text;
        }
    }
} 