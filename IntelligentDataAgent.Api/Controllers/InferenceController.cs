using IntelligentDataAgent.Core.Interfaces;
using IntelligentDataAgent.Core.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace IntelligentDataAgent.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InferenceController : ControllerBase
    {
        private readonly IInferenceModelService _inferenceModelService;
        private readonly ILogger<InferenceController> _logger;

        public InferenceController(
            IInferenceModelService inferenceModelService,
            ILogger<InferenceController> logger)
        {
            _inferenceModelService = inferenceModelService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> RunInference([FromBody] InferenceRequest request)
        {
            try
            {
                _logger.LogInformation($"Running inference with model {request.ModelId}");
                
                var result = await _inferenceModelService.InferAsync(request);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during inference");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpPost("load/{modelId}")]
        public async Task<IActionResult> LoadModel(string modelId)
        {
            try
            {
                _logger.LogInformation($"Loading model {modelId}");
                
                await _inferenceModelService.LoadModelAsync(modelId);
                
                return Ok(new { Success = true, Message = $"Model {modelId} loaded successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading model {modelId}");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpPost("unload/{modelId}")]
        public async Task<IActionResult> UnloadModel(string modelId)
        {
            try
            {
                _logger.LogInformation($"Unloading model {modelId}");
                
                await _inferenceModelService.UnloadModelAsync(modelId);
                
                return Ok(new { Success = true, Message = $"Model {modelId} unloaded successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error unloading model {modelId}");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpGet("status/{modelId}")]
        public async Task<IActionResult> GetModelStatus(string modelId)
        {
            try
            {
                var isLoaded = await _inferenceModelService.IsModelLoadedAsync(modelId);
                
                return Ok(new { ModelId = modelId, IsLoaded = isLoaded });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking model status {modelId}");
                return StatusCode(500, new { Error = ex.Message });
            }
        }
    }
} 