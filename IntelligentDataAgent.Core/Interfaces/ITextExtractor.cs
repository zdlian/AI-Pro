using System.Collections.Generic;
using System.Threading.Tasks;

namespace IntelligentDataAgent.Core.Interfaces
{
    public interface ITextExtractor
    {
        Task<string> ExtractTextFromHtmlAsync(string html);
        Task<Dictionary<string, string>> ExtractMetadataFromHtmlAsync(string html);
    }
} 