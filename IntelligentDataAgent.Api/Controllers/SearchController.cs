using IntelligentDataAgent.Core.Interfaces;
using IntelligentDataAgent.Core.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace IntelligentDataAgent.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly ISearchService _searchService;
        private readonly IElasticsearchService _elasticsearchService;
        private readonly ILogger<SearchController> _logger;

        public SearchController(
            ISearchService searchService,
            IElasticsearchService elasticsearchService,
            ILogger<SearchController> logger)
        {
            _searchService = searchService;
            _elasticsearchService = elasticsearchService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Search([FromBody] SearchRequest request)
        {
            try
            {
                _logger.LogInformation($"Searching in index {request.IndexName} with query: {request.Query}");
                
                var result = await _searchService.SearchAsync<Document>(request);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during search");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpGet("{indexName}/{id}")]
        public async Task<IActionResult> GetDocument(string indexName, string id)
        {
            try
            {
                _logger.LogInformation($"Getting document {id} from index {indexName}");
                
                var document = await _elasticsearchService.GetDocumentAsync<Document>(indexName, id);
                
                if (document == null)
                {
                    return NotFound(new { Error = $"Document {id} not found" });
                }
                
                return Ok(document);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting document {id}");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpGet("count/{indexName}")]
        public async Task<IActionResult> CountDocuments(string indexName, [FromQuery] string query = null)
        {
            try
            {
                _logger.LogInformation($"Counting documents in index {indexName}");
                
                var count = await _searchService.CountDocumentsAsync(indexName, query);
                
                return Ok(new { Count = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting documents");
                return StatusCode(500, new { Error = ex.Message });
            }
        }
    }
} 