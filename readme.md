# 基于推理模型的 .NET 8 Agent 方案

这个项目是一个基于 .NET 8 的智能数据处理系统，它集成了网页爬取、数据处理、Elasticsearch 索引和搜索以及 ONNX 推理模型功能。

## 项目结构

该解决方案包含以下项目：

1. **IntelligentDataAgent.Core**: 包含核心接口和模型定义
2. **IntelligentDataAgent.Crawlers**: 实现网页和API爬取功能
3. **IntelligentDataAgent.Processing**: 实现数据处理和规范化功能
4. **IntelligentDataAgent.Elasticsearch**: 实现Elasticsearch索引和搜索功能
5. **IntelligentDataAgent.Inference**: 实现ONNX推理模型功能
6. **LangChain.Agent**: 主控制台应用程序，协调各组件工作
7. **IntelligentDataAgent.Api**: Web API项目，提供HTTP接口

## 功能特点

- **数据爬取**：支持网页和API数据爬取，可配置爬取深度和最大页面数
- **数据处理**：自动提取文本内容和元数据，规范化数据格式
- **数据索引**：将处理后的数据索引到Elasticsearch，支持全文搜索
- **推理模型**：集成ONNX运行时，支持文本分类、实体提取和嵌入向量生成
- **定时任务**：支持配置定时爬取任务，自动更新数据
- **Web API**：提供RESTful API接口，方便集成到其他系统

## 环境要求

- .NET 8.0 SDK
- Elasticsearch 7.x 或更高版本
- ONNX Runtime 1.12.0 或更高版本

## 配置说明

在`appsettings.json`中配置以下内容：

1. **ElasticsearchSettings**: Elasticsearch连接设置
2. **CrawlerSettings**: 爬虫设置
3. **InferenceSettings**: 推理模型设置
4. **ScheduledCrawls**: 定时爬取任务配置

## 使用方法

### 1. 启动控制台应用程序

这将启动Agent协调器，它会：
- 预加载配置的推理模型
- 设置定时爬取任务
- 监控任务状态

### 2. 使用Web API

启动Web API项目：

```bash
cd IntelligentDataAgent.Api
dotnet run
```

API端点：

- **POST /api/crawl**: 手动启动爬取任务
- **POST /api/crawl/schedule**: 调度爬取任务
- **DELETE /api/crawl/{jobId}**: 取消爬取任务
- **POST /api/search**: 搜索文档
- **GET /api/search/{indexName}/{id}**: 获取特定文档
- **GET /api/search/count/{indexName}**: 获取文档数量
- **POST /api/inference**: 运行推理
- **POST /api/inference/load/{modelId}**: 加载模型
- **POST /api/inference/unload/{modelId}**: 卸载模型

## 示例请求

### 启动爬取任务

```json
POST /api/crawl
{
  "sources": ["https://example.com", "https://example.org"],
  "maxDepth": 2,
  "maxPages": 100,
  "crawlType": "Web"
}
```

### 搜索文档

```json
POST /api/search
{
  "indexName": "intelligent_agent_index",
  "query": "artificial intelligence",
  "from": 0,
  "size": 10,
  "filters": [
    {
      "field": "source",
      "value": "https://example.com"
    }
  ]
}
```

### 运行推理

```json
POST /api/inference
{
  "modelId": "text-classification-model",
  "modelType": "TextClassification",
  "inputs": {
    "text": "This is a sample text for classification."
  }
}
```

## 开发指南

### 添加新的爬虫

1. 创建新的爬虫类，实现 `IWebCrawler` 或 `IApiCrawler` 接口
2. 在 `Program.cs` 中注册新的爬虫
3. 在 `CrawlerManager` 中添加对新爬虫的支持

### 添加新的推理模型

1. 将ONNX模型文件放入 `Models` 目录
2. 在 `appsettings.json` 中配置模型路径
3. 在 `InferenceModelService` 中添加对新模型的支持

## 许可证

MIT

## 6. 总结

我们已经完成了基于推理模型的 .NET 8 Agent 方案的实现，包括：

1. 修复了 TextExtractor 和 DataNormalizer 类中的问题
2. 创建了 ISearchService 接口和实现
3. 创建了 IIndexManager 接口和实现
4. 创建了详细的 README.md 文件

该系统现在可以：
- 爬取网页和API数据
- 处理和规范化数据
- 索引数据到Elasticsearch
- 使用ONNX模型进行推理
- 通过Web API提供服务

系统设计遵循了依赖注入和接口分离原则，使各组件松耦合且易于测试。通过配置文件可以灵活调整系统行为，满足不同的应用场景需求。