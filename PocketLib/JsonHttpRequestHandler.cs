using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PocketLib
{
    public class JsonHttpRequestHandler : IHttpRequestHandler
    {
        private readonly HttpClient _client;

        public JsonHttpRequestHandler(HttpClient client)
        {
            _client = client;
        }

        public async Task<T> PostAsync<T>(string path, object reqParams)
        {
            var req = new HttpRequestMessage(HttpMethod.Post, path);
            var body = JsonConvert.SerializeObject(reqParams);
            req.Content = new StringContent(body, Encoding.UTF8, "application/json");
            req.Headers.Add("X-Accept", "application/json");

            var result = await _client.SendAsync(req);
            if (!result.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Unexpected response {result}");
            }
            var responseString = await result.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(responseString);
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}
