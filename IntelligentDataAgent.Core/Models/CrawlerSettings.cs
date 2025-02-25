namespace IntelligentDataAgent.Core.Models
{
    public class CrawlerSettings
    {
        public string UserAgent { get; set; }
        public int RequestTimeoutSeconds { get; set; }
        public int MaxConcurrentRequests { get; set; }
        public bool RespectRobotsTxt { get; set; }
        public int DelayBetweenRequestsMs { get; set; }
        public double RequestTimeout { get; set; }
    }
} 