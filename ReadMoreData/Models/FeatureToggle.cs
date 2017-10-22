using System;
using System.Diagnostics.CodeAnalysis;

namespace ReadMoreData.Models
{
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class FeatureToggle
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
}