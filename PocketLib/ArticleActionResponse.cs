using Newtonsoft.Json;

namespace PocketLib
{
    public class ArticleActionResponse
    {
        [JsonProperty("action_results")]
        public bool[] ActionResults { get; private set; }
        [JsonProperty("status"), JsonConverter(typeof(BoolConverter))]
        public bool Status { get; private set; }

        [JsonConstructor]
        public ArticleActionResponse(bool[] actionResults, bool status)
        {
            ActionResults = actionResults;
            Status = status;
        }
    }
}