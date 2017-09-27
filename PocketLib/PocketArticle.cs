using System;
using Newtonsoft.Json;

namespace PocketLib
{
    public class PocketArticle
    {
        [JsonProperty("item_id")]
        public string ItemId { get; private set; }
        [JsonProperty("resolved_url")]
        public Uri Url { get; private set; }
        [JsonProperty("resolved_title")]
        public string Title { get; private set; }

        [JsonConstructor]
        public PocketArticle(string itemId, Uri url, string title)
        {
            ItemId = itemId;
            Url = url;
            Title = title;
        }
    }
}