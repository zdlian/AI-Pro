using IntelligentDataAgent.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IntelligentDataAgent.Processing
{
    public class DataNormalizer : IDataNormalizer
    {
        private readonly ILogger<DataNormalizer> _logger;

        public DataNormalizer(ILogger<DataNormalizer> logger)
        {
            _logger = logger;
        }

        public string NormalizeText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;
            
            try
            {
                // 移除多余的空白字符
                text = Regex.Replace(text, @"\s+", " ");
                
                // 移除特殊字符
                text = Regex.Replace(text, @"[^\w\s.,;:!?()[\]{}\-'""]+", " ");
                
                // 移除重复的标点符号
                text = Regex.Replace(text, @"([.,;:!?])+", "$1");
                
                // 确保句子之间有适当的空格
                text = Regex.Replace(text, @"([.!?])\s*(\w)", "$1 $2");
                
                // 移除前后空白
                text = text.Trim();
                
                return text;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error normalizing text");
                return text;
            }
        }

        public Task<string> NormalizeTextAsync(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return Task.FromResult(string.Empty);
            }

            try
            {
                // 移除多余的空白字符
                var normalizedText = Regex.Replace(text, @"\s+", " ");
                
                // 移除多余的换行符
                normalizedText = Regex.Replace(normalizedText, @"\n+", "\n");
                
                // 移除HTML标签（如果有）
                normalizedText = Regex.Replace(normalizedText, @"<[^>]+>", "");
                
                // 移除URL
                normalizedText = Regex.Replace(normalizedText, @"https?://\S+", "");
                
                // 移除特殊字符
                normalizedText = Regex.Replace(normalizedText, @"[^\w\s.,;:!?()[\]{}\-'""]+", " ");
                
                // 移除重复的标点符号
                normalizedText = Regex.Replace(normalizedText, @"([.,;:!?])+", "$1");
                
                // 确保句子之间有适当的空格
                normalizedText = Regex.Replace(normalizedText, @"([.!?])\s*(\w)", "$1 $2");
                
                // 规范化空白字符
                normalizedText = normalizedText.Trim();
                
                // 分段处理
                var paragraphs = Regex.Split(normalizedText, @"\n");
                var sb = new StringBuilder();
                
                foreach (var paragraph in paragraphs)
                {
                    var trimmedParagraph = paragraph.Trim();
                    if (!string.IsNullOrWhiteSpace(trimmedParagraph))
                    {
                        sb.AppendLine(trimmedParagraph);
                        sb.AppendLine();
                    }
                }
                
                return Task.FromResult(sb.ToString().Trim());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error normalizing text");
                return Task.FromResult(text);
            }
        }
    }
} 