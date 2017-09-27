using System;
using PocketLib;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Moq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;

namespace PocketLibTests
{
    [TestClass]
    public class HttpPocketClientTests
    {
        private const string AccessToken = "pocket-access-token";
        private const string UserName = "pocket-user-name";
        private const string RequestCode = "pocket-request-code";
        private const string ConsumerKey = "pocket-consumer-key";
        private static readonly Uri RedirectUri = new Uri("http://example.com/a?b=c");
        private readonly Dictionary<string, PocketArticle> _articles = new Dictionary<string, PocketArticle> {
            { "1", new PocketArticle("1", new Uri("http://example.com"), "Example 1") },
            { "2", new PocketArticle("2", new Uri("http://example.com"), "Example 2") },
            { "3", new PocketArticle("3", new Uri("http://example.com"), "Example 3") },
            { "4", new PocketArticle("4", new Uri("http://example.com"), "Example 4") }
        };
        private readonly Mock<IHttpRequestHandler> _reqHandler;
        private readonly HttpPocketClient _service;

        public HttpPocketClientTests()
        {
            _reqHandler = new Mock<IHttpRequestHandler>();

            _service = new HttpPocketClient(_reqHandler.Object, ConsumerKey);
        }

        [TestMethod]
        public async Task SendsConsumerKeyAndRedirectUriWhenCreatingRequestCode()
        {
            SetupResponse(new RequestCodeResponse(RequestCode));

            await _service.CreateRequestCodeAsync(RedirectUri);

            DidRequest<RequestCodeResponse>(
                "https://getpocket.com/v3/oauth/request",
                new { consumer_key = ConsumerKey, redirect_uri = RedirectUri });
        }

        [TestMethod]
        public async Task ReturnsRequestCodeFromResponse()
        {
            const string expectedRequestCode = "request-code-from-response";
            SetupResponse(new RequestCodeResponse(expectedRequestCode));

            var result = await _service.CreateRequestCodeAsync(RedirectUri);

            Assert.AreEqual(expectedRequestCode, result.Code);
        }

        [TestMethod]
        public async Task RequestCodeCanBeConvertedToAuthUrl()
        {
            var expectedAuthUrl = new Uri($"https://getpocket.com/auth/authorize?request_token={RequestCode}&redirect_uri={RedirectUri}");
            SetupResponse(new RequestCodeResponse(RequestCode));
            var requestCode = await _service.CreateRequestCodeAsync(RedirectUri);

            var result = requestCode.ToAuthUrl();

            Assert.AreEqual(expectedAuthUrl, result);
        }

        [TestMethod]
        public async Task SendsConsumerKeyAndRequestCodeWhenCreatingAccessToken()
        {
            SetupResponse(new AccessTokenResponse(AccessToken, UserName));

            await _service.CreateAccessTokenAsync(RequestCode);

            DidRequest<AccessTokenResponse>(
                "https://getpocket.com/v3/oauth/authorize",
                new Dictionary<string, object> { { "consumer_key", ConsumerKey }, { "code", RequestCode } });
        }

        [TestMethod]
        public async Task ReturnsAccessTokenFromResponse()
        {
            SetupResponse(new AccessTokenResponse(AccessToken, UserName));

            var result = await _service.CreateAccessTokenAsync(RequestCode);

            Assert.AreEqual(AccessToken, result);
        }

        [TestMethod]
        public async Task SendsParamsWhenRetrievingArticles()
        {
            SetupResponse(new PocketArticles(_articles));

            await _service.GetRandomArticleAsync(AccessToken);

            DidRequest<PocketArticles>(
                "https://getpocket.com/v3/get",
                new {
                    consumer_key = ConsumerKey,
                    access_token = AccessToken,
                    count = 200,
                    detailType = "simple"
                });
        }

        [TestMethod]
        public async Task RetrievesRequestedNumberOfArticlesToChooseFrom()
        {
            const int countToRetrieve = 345;
            SetupResponse(new PocketArticles(_articles));

            await _service.GetRandomArticleAsync(AccessToken, countToRetrieve);

            DidRequest<PocketArticles>(
                "https://getpocket.com/v3/get",
                new
                {
                    consumer_key = ConsumerKey,
                    access_token = AccessToken,
                    count = countToRetrieve,
                    detailType = "simple"
                });
        }

        [TestMethod]
        public async Task ReturnsArticleFromResults()
        {
            SetupResponse(new PocketArticles(_articles));

            var result = await _service.GetRandomArticleAsync(AccessToken);

            Assert.IsTrue(_articles.ContainsValue(result));
        }

        [TestMethod]
        public async Task ReturnsRandomArticleFromResults()
        {
            SetupResponse(new PocketArticles(_articles));

            var results = new[]
            {
                await _service.GetRandomArticleAsync(AccessToken),
                await _service.GetRandomArticleAsync(AccessToken),
                await _service.GetRandomArticleAsync(AccessToken),
                await _service.GetRandomArticleAsync(AccessToken)
            };

            // Check that the articles aren't all the same
            Assert.IsTrue(results.Distinct().Count() > 1, "All the articles were the same");
        }

        [TestMethod]
        public async Task SendsParamsWhenDeletingArticle()
        {
            const string articleId = "pocket-article-id";
            SetupResponse(new ArticleActionResponse(new [] { true }, true));

            await _service.DeleteArticleAsync(AccessToken, articleId);
            
            DidRequest<ArticleActionResponse>(
                "https://getpocket.com/v3/send",
                new {
                    consumer_key = ConsumerKey,
                  access_token = AccessToken,
                  actions = new[]{ new {
                        action = "delete",
                        item_id = articleId
                    }}
                });
        }

        [TestMethod]
        public async Task SendsParamsWhenArchivingArticle()
        {
            const string articleId = "pocket-article-id";
            SetupResponse(new ArticleActionResponse(new[] { true }, true));

            await _service.ArchiveArticleAsync(AccessToken, articleId);

            DidRequest<ArticleActionResponse>(
                "https://getpocket.com/v3/send",
                new
                {
                    consumer_key = ConsumerKey,
                    access_token = AccessToken,
                    actions = new[]{ new {
                        action = "archive",
                        item_id = articleId
                    }}
                });
        }

        [TestMethod]
        public void DisposingTheClientDisposesTheRequestHandler()
        {
            _service.Dispose();

            _reqHandler.Verify(r => r.Dispose(), Times.Exactly(1));
        }

        [TestMethod]
        [ExpectedException(typeof(PocketException))]
        public async Task ThrowsPocketExceptionWhenFailToCreateRequestCode()
        {
            _reqHandler.Setup(r => r.PostAsync<RequestCodeResponse>(It.IsAny<string>(), It.IsAny<object>())).ThrowsAsync(new WebException());

            await _service.CreateRequestCodeAsync(new Uri("http://uri/"));
        }

        [TestMethod]
        [ExpectedException(typeof(PocketException))]
        public async Task ThrowsPocketExceptionWhenFailToCreateAccessToken()
        {
            _reqHandler.Setup(r => r.PostAsync<AccessTokenResponse>(It.IsAny<string>(), It.IsAny<object>())).ThrowsAsync(new WebException());

            await _service.CreateAccessTokenAsync("");
        }

        [TestMethod]
        [ExpectedException(typeof(PocketException))]
        public async Task ThrowsPocketExceptionWhenFailToGetRandomArticle()
        {
            _reqHandler.Setup(r => r.PostAsync<PocketArticles>(It.IsAny<string>(), It.IsAny<object>())).ThrowsAsync(new WebException());

            await _service.GetRandomArticleAsync("");
        }

        [TestMethod]
        [ExpectedException(typeof(PocketException))]
        public async Task ThrowsPocketExceptionWhenFailToDeleteArticle()
        {
            _reqHandler.Setup(r => r.PostAsync<ArticleActionResponse>(It.IsAny<string>(), It.IsAny<object>())).ThrowsAsync(new WebException());

            await _service.DeleteArticleAsync("", "");
        }

        [TestMethod]
        [ExpectedException(typeof(PocketException))]
        public async Task ThrowsPocketExceptionWhenFailToArchiveArticle()
        {
            _reqHandler.Setup(r => r.PostAsync<ArticleActionResponse>(It.IsAny<string>(), It.IsAny<object>())).ThrowsAsync(new WebException());

            await _service.ArchiveArticleAsync("", "");
        }

        private void SetupResponse<T>(T response)
        {
            _reqHandler.Setup(r => r.PostAsync<T>(It.IsAny<string>(), It.IsAny<object>())).Returns(Task.FromResult(response));
        }

        // ReSharper disable once UnusedParameter.Local
        private void DidRequest<T>(string path, object expectedParams)
        {
            _reqHandler.Verify(r => r.PostAsync<T>(
                    It.Is<string>(p => path.Equals(p)),
                    It.Is<object>(p => JsonConvert.SerializeObject(expectedParams).Equals(JsonConvert.SerializeObject(p)))),
                Times.Exactly(1));
        }
    }
}
