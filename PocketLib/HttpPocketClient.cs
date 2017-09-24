using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PocketLib
{
    public class HttpPocketClient : IPocketClient, IDisposable
    {
        private const string BaseUri = "https://getpocket.com";
        private const string ApiVersion = "v3";
        private readonly Random _random;
        private readonly IHttpRequestHandler _reqHandler;
        private readonly string _consumerKey;

        public HttpPocketClient(IHttpRequestHandler reqHandler, string consumerKey)
        {
            _random = new Random();
            _reqHandler = reqHandler;
            _consumerKey = consumerKey;
        }

        public async Task<PocketRequestCode> CreateRequestCodeAsync(string redirectUri)
        {
            var reqParams = new Dictionary<string, object>
            {
                { "consumer_key", _consumerKey },
                { "redirect_uri", redirectUri }
            };
            var response = await SafePostAsync<RequestCodeResponse>($"{BaseUri}/{ApiVersion}/oauth/request", reqParams);

            return new PocketRequestCode(BaseUri, redirectUri, response.Code);
        }

        public async Task<string> CreateAccessTokenAsync(string requestCode)
        {
            var reqParams = new Dictionary<string, object>
            {
                { "consumer_key", _consumerKey },
                { "code", requestCode }
            };
            var response = await SafePostAsync<AccessTokenResponse>($"{BaseUri}/{ApiVersion}/oauth/authorize", reqParams);

            return response.AccessToken;
        }

        public async Task<PocketArticle> GetRandomArticleAsync(string accessToken, int countToRetrieve = 200)
        {
            var reqParams = new Dictionary<string, object>
            {
                { "consumer_key", _consumerKey },
                { "access_token", accessToken },
                { "count", countToRetrieve },
                { "detailType", "simple" }
            };
            var response = await SafePostAsync<PocketArticles>($"{BaseUri}/{ApiVersion}/get", reqParams);

            var articleIndex = _random.Next(response.Articles.Count);
            return response.Articles.Values.ElementAt(articleIndex);
        }

        public async Task DeleteArticleAsync(string accessToken, string articleId)
        {
            var reqParams = new Dictionary<string, object>
            {
                { "consumer_key", _consumerKey },
                { "access_token", accessToken },
                { "actions", new [] { new {
                    action = "delete",
                    item_id = articleId
                }}}
            };
            await SafePostAsync<ArticleActionResponse>($"{BaseUri}/{ApiVersion}/send", reqParams);
        }

        public async Task ArchiveArticleAsync(string accessToken, string articleId)
        {
            var reqParams = new Dictionary<string, object>
            {
                { "consumer_key", _consumerKey },
                { "access_token", accessToken },
                { "actions", new [] { new {
                    action = "archive",
                    item_id = articleId
                }}}
            };
            await SafePostAsync<ArticleActionResponse>($"{BaseUri}/{ApiVersion}/send", reqParams);
        }

        public void Dispose()
        {
            _reqHandler.Dispose();
        }

        private async Task<T> SafePostAsync<T>(string path, object reqParams)
        {
            try
            {
                return await _reqHandler.PostAsync<T>(path, reqParams);
            } catch(Exception e)
            {
                throw new PocketException("Failed to execute request", e);
            }
        }
    }
}
