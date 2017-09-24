using Newtonsoft.Json;

namespace PocketLib
{
    public class AccessTokenResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; private set; }

        [JsonProperty("username")]
        public string Username { get; private set; }

        [JsonConstructor]
        public AccessTokenResponse(string accessToken, string username)
        {
            AccessToken = accessToken;
            Username = username;
        }
    }
}