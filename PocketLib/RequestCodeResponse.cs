using Newtonsoft.Json;

namespace PocketLib
{
    public class RequestCodeResponse
    {
        [JsonProperty("code")]
        public string Code { get; private set; }

        [JsonConstructor]
        public RequestCodeResponse(string code)
        {
            Code = code;
        }
    }
}