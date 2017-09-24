using System.Collections.Generic;
using Newtonsoft.Json;

namespace PocketLib
{
    public class PocketArticles
    {
        [JsonProperty("list")]
        public Dictionary<string, PocketArticle> Articles { get; private set; }

        [JsonConstructor]
        public PocketArticles(Dictionary<string, PocketArticle> articles)
        {
            Articles = articles;
        }
    }
}