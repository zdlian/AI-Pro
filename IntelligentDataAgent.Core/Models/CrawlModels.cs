using System;
using System.Collections.Generic;

namespace IntelligentDataAgent.Core.Models
{
    public enum CrawlType
    {
        Web,
        Api,
        Mixed
    }

    public enum JobStatus
    {
        Scheduled,
        Running,
        Completed,
        Failed,
        Cancelled
    }

    public class CrawlRequest
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public List<string> Sources { get; set; } = new List<string>();
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();
        public int MaxDepth { get; set; } = 1;
        public int MaxPages { get; set; } = 10;
        public CrawlType CrawlType { get; set; } = CrawlType.Web;
    }

    public class CrawlSchedule
    {
        public string Name { get; set; }
        public string CronExpression { get; set; }
        public List<string> Sources { get; set; } = new List<string>();
        public int MaxDepth { get; set; } = 1;
        public int MaxPages { get; set; } = 10;
        public CrawlType CrawlType { get; set; } = CrawlType.Web;
    }

    public class CrawlJob
    {
        public string Id { get; set; }
        public CrawlSchedule Schedule { get; set; }
        public JobStatus Status { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class CrawledData
    {
        public string Id { get; set; }
        public string Url { get; set; }
        public string Content { get; set; }
        public string ContentType { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
        public DateTime CrawlTime { get; set; }
    }

    public class CrawlJobStatus
    {
        public string JobId { get; set; }
        public string Status { get; set; }
        public DateTime? LastRunTime { get; set; }
        public DateTime? NextRunTime { get; set; }
        public string ErrorMessage { get; set; }

        public static CrawlJobStatus Scheduled => new CrawlJobStatus { Status = "Scheduled" };
        public static CrawlJobStatus Running => new CrawlJobStatus { Status = "Running" };
        public static CrawlJobStatus Completed => new CrawlJobStatus { Status = "Completed" };
        public static CrawlJobStatus Failed => new CrawlJobStatus { Status = "Failed" };
    }
} 