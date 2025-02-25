using System;
using System.Collections.Generic;

namespace IntelligentDataAgent.Core.Models
{
    public enum FilterType
    {
        Term,
        Range,
        Exists,
        Wildcard
    }

    public class Filter
    {
        public string Field { get; set; }
        public FilterType Type { get; set; }
        public object Value { get; set; }
    }

    public class RangeFilter : Filter
    {
        public double From { get; set; }
        public double To { get; set; }
    }

    public class SortField
    {
        public string FieldName { get; set; }
        public bool Ascending { get; set; }
    }

    public class SearchRequest
    {
        public string IndexName { get; set; }
        public string Query { get; set; }
        public bool SortAscending { get; set; } = true;
        public List<Filter> Filters { get; set; } = new List<Filter>();
        public List<SortField> SortFields { get; set; }
        public int From { get; set; }
        public int Size { get; set; }
    }

    public class SearchResult<T> where T : class
    {
        public long Total { get; set; }
        public long Took { get; set; }
        public long TotalHits { get; set; }
        public bool TimedOut { get; set; }
        public List<T> Documents { get; set; } = new List<T>();
        public Dictionary<string, List<Facet>> Facets { get; set; } = new Dictionary<string, List<Facet>>();
    }

    public class Facet
    {
        public string Value { get; set; }
        public long Count { get; set; }
    }

    public class Entity
    {
        public string Text { get; set; }
        public string Type { get; set; }
        public int StartPosition { get; set; }
        public int EndPosition { get; set; }
        public double Confidence { get; set; }
    }
} 