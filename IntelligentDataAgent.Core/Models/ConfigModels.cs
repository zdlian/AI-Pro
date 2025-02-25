using System;
using System.Collections.Generic;

namespace IntelligentDataAgent.Core.Models
{
  
    public class ScheduledCrawl
    {
        public string Name { get; set; }
        public string CronExpression { get; set; }
        public List<string> Sources { get; set; }
        public int MaxDepth { get; set; }
        public int MaxPages { get; set; }
    }
} 