using IntelligentDataAgent.Core.Interfaces;
using IntelligentDataAgent.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace IntelligentDataAgent.Inference
{
    public class InferenceModelService : IInferenceModelService, IDisposable
    {
        private readonly ILogger<InferenceModelService> _logger;
        private readonly InferenceSettings _settings;
        private readonly ConcurrentDictionary<string, InferenceSession> _loadedModels = new();

        public InferenceModelService(
            IOptions<InferenceSettings> settings,
            ILogger<InferenceModelService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<InferenceResult> InferAsync(InferenceRequest request)
        {
            _logger.LogInformation($"Running inference with model {request.ModelId}");
            
            // 确保模型已加载
            if (!_loadedModels.ContainsKey(request.ModelId))
            {
                await LoadModelAsync(request.ModelId);
            }
            
            if (!_loadedModels.TryGetValue(request.ModelId, out var session))
            {
                throw new Exception($"Failed to load model {request.ModelId}");
            }
            
            // 准备输入
            var inputTensors = new List<NamedOnnxValue>();
            
            foreach (var input in request.Inputs)
            {
                if (input.Value is string textInput)
                {
                    // 处理文本输入
                    var encodedText = EncodeText(textInput);
                    inputTensors.Add(NamedOnnxValue.CreateFromTensor(input.Key, encodedText));
                }
                else if (input.Value is float[] floatArray)
                {
                    // 处理浮点数组输入
                    var tensor = new DenseTensor<float>(floatArray, new[] { 1, floatArray.Length });
                    inputTensors.Add(NamedOnnxValue.CreateFromTensor(input.Key, tensor));
                }
                else if (input.Value is int[] intArray)
                {
                    // 处理整数数组输入
                    var tensor = new DenseTensor<int>(intArray, new[] { 1, intArray.Length });
                    inputTensors.Add(NamedOnnxValue.CreateFromTensor(input.Key, tensor));
                }
                else
                {
                    throw new NotSupportedException($"Unsupported input type for {input.Key}");
                }
            }
            
            // 运行推理
            var outputs = session.Run(inputTensors);
            
            // 处理输出
            var result = new InferenceResult
            {
                ModelId = request.ModelId,
                ModelType = request.ModelType,
                Outputs = new Dictionary<string, object>()
            };
            
            foreach (var output in outputs)
            {
                switch (output.ElementType)
                {
                    case TensorElementType.Float:
                        var floatTensor = output.AsTensor<float>();
                        result.Outputs[output.Name] = floatTensor.ToArray();
                        break;
                    case TensorElementType.Int32:
                        var intTensor = output.AsTensor<int>();
                        result.Outputs[output.Name] = intTensor.ToArray();
                        break;
                    case TensorElementType.String:
                        var stringTensor = output.AsTensor<string>();
                        result.Outputs[output.Name] = stringTensor.ToArray();
                        break;
                    default:
                        _logger.LogWarning($"Unsupported output type: {output.ElementType}");
                        break;
                }
            }
            
            return result;
        }

        public async Task LoadModelAsync(string modelId)
        {
            _logger.LogInformation($"Loading model {modelId}");
            
            try
            {
                // 如果模型已加载，则直接返回
                if (_loadedModels.ContainsKey(modelId))
                {
                    _logger.LogInformation($"Model {modelId} already loaded");
                    return;
                }
                
                // 查找模型配置
                var modelConfig = _settings.Models.FirstOrDefault(m => m.Id == modelId);
                if (modelConfig == null)
                {
                    throw new Exception($"Model configuration for {modelId} not found");
                }
                
                // 构建模型路径
                var modelPath = Path.Combine(_settings.ModelsDirectory, modelConfig.Path);
                if (!File.Exists(modelPath))
                {
                    throw new Exception($"Model file not found: {modelPath}");
                }
                
                // 创建会话选项
                var sessionOptions = new SessionOptions();
                
                // 设置执行提供程序
                switch (modelConfig.ExecutionProvider?.ToUpperInvariant())
                {
                    case "CUDA":
                        // 如果有CUDA支持，则使用CUDA
                        sessionOptions.AppendExecutionProvider_CUDA();
                        break;
                    case "DIRECTML":
                        // 如果有DirectML支持，则使用DirectML
                        // 注意：需要安装Microsoft.ML.OnnxRuntime.DirectML包
                        // sessionOptions.AppendExecutionProvider_DML();
                        _logger.LogWarning("DirectML provider requested but not supported in this build. Falling back to CPU.");
                        sessionOptions.AppendExecutionProvider_CPU();
                        break;
                    case "TENSORRT":
                        // 如果有TensorRT支持，则使用TensorRT
                        // 注意：需要安装特殊的TensorRT包
                        // sessionOptions.AppendExecutionProvider_TensorRT();
                        _logger.LogWarning("TensorRT provider requested but not supported in this build. Falling back to CPU.");
                        sessionOptions.AppendExecutionProvider_CPU();
                        break;
                    default:
                        // 默认使用CPU
                        sessionOptions.AppendExecutionProvider_CPU();
                        break;
                }
                
                // 加载模型
                var session = new InferenceSession(modelPath, sessionOptions);
                
                // 将模型添加到已加载模型字典
                if (_loadedModels.TryAdd(modelId, session))
                {
                    _logger.LogInformation($"Successfully loaded model {modelId}");
                }
                else
                {
                    session.Dispose();
                    _logger.LogWarning($"Model {modelId} already loaded");
                }
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading model {modelId}");
                throw;
            }
        }

        public Task UnloadModelAsync(string modelId)
        {
            _logger.LogInformation($"Unloading model {modelId}");
            
            if (_loadedModels.TryRemove(modelId, out var session))
            {
                session.Dispose();
                _logger.LogInformation($"Successfully unloaded model {modelId}");
            }
            else
            {
                _logger.LogWarning($"Model {modelId} not found or already unloaded");
            }
            
            return Task.CompletedTask;
        }

        public Task<bool> IsModelLoadedAsync(string modelId)
        {
            var isLoaded = _loadedModels.ContainsKey(modelId);
            _logger.LogDebug($"Model {modelId} is {(isLoaded ? "loaded" : "not loaded")}");
            return Task.FromResult(isLoaded);
        }

        private Tensor<float> EncodeText(string text)
        {
            // 简单的文本编码实现，实际应用中可能需要更复杂的分词和编码逻辑
            var tokens = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var encodedText = new float[tokens.Length];
            
            for (int i = 0; i < tokens.Length; i++)
            {
                // 简单的哈希编码，实际应用中应使用适当的词嵌入或分词器
                encodedText[i] = tokens[i].GetHashCode() % 10000 / 10000.0f;
            }
            
            return new DenseTensor<float>(encodedText, new[] { 1, encodedText.Length });
        }

        public void Dispose()
        {
            foreach (var model in _loadedModels.Values)
            {
                model.Dispose();
            }
            
            _loadedModels.Clear();
        }
    }
} 