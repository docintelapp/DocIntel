using System;
using System.Collections.Generic;

namespace DocIntel.Integrations.PassiveTotal.Model
{
    public class Article
    {
        public string Guid { get; set; }
        public string Title { get; set; }
        public string Summary { get; set; }
        public string Type { get; set; }
        public DateTime PublishedDate { get; set; }
        public string Link { get; set; }
        public IEnumerable<string> Categories { get; set; }
        public IEnumerable<string> Tags { get; set; }
        public IEnumerable<Indicator> Indicators { get; set; }
    }

    public class Indicator
    {
        public string Type { get; set; }
        public int Count { get; set; }
        public IEnumerable<string> Values { get; set; }
        public string Public { get; set; }
    }
}