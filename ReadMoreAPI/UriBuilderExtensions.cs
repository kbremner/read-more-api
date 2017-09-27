using System;

namespace ReadMoreAPI
{
    public static class UriBuilderExtensions
    {
        public static void AppendToQuery(this UriBuilder uri, string key, string value)
        {
            if (string.IsNullOrWhiteSpace(uri.Query))
            {
                uri.Query = $"{key}={value}";
            }
            else
            {
                uri.Query += $"&{key}={value}";
            }
        }
    }
}
