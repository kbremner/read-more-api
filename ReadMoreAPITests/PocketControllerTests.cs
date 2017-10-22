using System;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReadMoreAPI.Controllers;
using ReadMoreAPI.Services;
using Microsoft.AspNetCore.Mvc.Routing;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PocketLib;
using ReadMoreAPI;
using ReadMoreData.Models;

namespace ReadMoreAPITests
{
    [TestClass]
    public class PocketControllerTests
    {
        private static readonly Uri PocketUrl = new Uri("http://pocket-url/");
        private static readonly Uri ActionUrl = new Uri("http://action-url/");
        private const string AccessToken = "ACCESS_TOKEN";
        private static readonly Uri UrlWithAccessToken = new Uri("http://url-with-access-token");
        private const string PocketAccessToken = "POCKET_ACCESS_TOKEN";
        private const string CallerUrl = "CALLER_URL";
        private const string ArticleId = "ITEM_ID";
        private static readonly Uri ArticleUrl = new Uri("http://article-url/");
        private const string ArticleTitle = "ARTICLE_TITLE";
        private const string DeleteUrl = "DELETE_URL";
        private const string ArchiveUrl = "ARCHIVE_URL";
        private PocketArticle _article;
        private PocketAccount _account;
        private Mock<IPocketService> _mockService;
        private Mock<IUrlHelper> _mockUrlHelper;
        private PocketController _controller;

        [TestInitialize]
        public void Setup()
        {
            _article = new PocketArticle(ArticleId, ArticleUrl, ArticleTitle);

            _mockService = new Mock<IPocketService>();
            _mockService.Setup(s => s.GenerateAuthUrlAsync(It.IsAny<Uri>(), It.IsAny<Uri>())).Returns(Task.FromResult(PocketUrl));

            _mockUrlHelper = new Mock<IUrlHelper>();
            _mockUrlHelper.Setup(s => s.Action(It.Is<UrlActionContext>(c => c.Action == "CompleteAuth"))).Returns(ActionUrl.ToString());
            _mockUrlHelper.Setup(s => s.Action(It.Is<UrlActionContext>(c => c.Action == "DeleteArticleAsync"))).Returns(DeleteUrl);
            _mockUrlHelper.Setup(s => s.Action(It.Is<UrlActionContext>(c => c.Action == "ArchiveArticleAsync"))).Returns(ArchiveUrl);

            _account = new PocketAccount { RedirectUrl = CallerUrl, AccessToken = PocketAccessToken };
            _mockService.Setup(s => s.UpgradeRequestTokenAsync(AccessToken)).ReturnsAsync(UrlWithAccessToken);
            _mockService.Setup(s => s.GetNextArticleAsync(AccessToken)).ReturnsAsync(_article);

            _controller = new PocketController(Mock.Of<ILogger<PocketController>>(), _mockService.Object)
            {
                Url = _mockUrlHelper.Object,
                // Set a HttpContext so that the Request property is not null when tests are running
                ControllerContext = {HttpContext = new DefaultHttpContext()}
            };
        }

        [TestMethod]
        public async Task AuthorizeGetsUrlFromPocketService()
        {
            var expectedUri = new Uri("http://uri/");
            await _controller.Authorize(expectedUri.ToString());

            _mockService.Verify(s => s.GenerateAuthUrlAsync(ActionUrl, expectedUri));
        }

        [TestMethod]
        public async Task AuthorizeRedirectsUser()
        {
            var expectedUri = new Uri("http://uri/");
            var result = await _controller.Authorize(expectedUri.ToString());

            Assert.IsInstanceOfType(result, typeof(RedirectResult));
        }

        [TestMethod]
        public async Task AuthorizeRedirectsToUrlFromPocketService()
        {
            var expectedUri = new Uri("http://uri/");
            var result = (RedirectResult) await _controller.Authorize(expectedUri.ToString());

            Assert.AreEqual(PocketUrl, result.Url);
        }

        [TestMethod]
        public async Task CompleteAuthCallsServiceToCreatePocketAccessToken()
        {
            await _controller.CompleteAuth(AccessToken);

            _mockService.Verify(s => s.UpgradeRequestTokenAsync(AccessToken), Times.Exactly(1));
        }

        [TestMethod]
        public async Task CompleteAuthRedirectsUser()
        {
            var result = await _controller.CompleteAuth(AccessToken);

            Assert.IsInstanceOfType(result, typeof(RedirectResult));
        }

        [TestMethod]
        public async Task CompleteAuthRedirectsToUrlWithAccessToken()
        {
            var result = (RedirectResult) await _controller.CompleteAuth(AccessToken);

            Assert.AreEqual(UrlWithAccessToken, result.Url);
        }

        [TestMethod]
        public async Task FetchingNextArticleReturnsArticleUrlInResponse()
        {
            var result = await _controller.GetNextArticleAsync(AccessToken);

            Assert.IsInstanceOfType(result, typeof(JsonResult));
            var jsonResult = (JsonResult) result;
            Assert.AreEqual(ArticleUrl, jsonResult.Value.GetProperty("url"));
        }

        [TestMethod]
        public async Task FetchingNextArticleReturnsForbiddenResponseIfFailsToGetNextArticle()
        {
            _mockService.Setup(s => s.GetNextArticleAsync(AccessToken)).Throws<PocketException>();

            var result = await _controller.GetNextArticleAsync(AccessToken);

            Assert.IsInstanceOfType(result, typeof(ObjectResult));
            var objectResult = (ObjectResult)result;
            Assert.AreEqual(403, objectResult.StatusCode);
        }

        [TestMethod]
        public async Task FetchingNextArticleReturnsActionsObjectWithUrlToDeleteArticleInResponse()
        {
            var result = await _controller.GetNextArticleAsync(AccessToken);

            Assert.IsInstanceOfType(result, typeof(JsonResult));
            var jsonResult = (JsonResult)result;
            var actions = jsonResult.Value.GetProperty("actions");
            Assert.AreEqual(DeleteUrl, actions.GetProperty("delete"));
        }

        [TestMethod]
        public async Task UrlToDeleteArticleIncludesArticleId()
        {
            await _controller.GetNextArticleAsync(AccessToken);

            _mockUrlHelper.Verify(
                u => u.Action(It.Is<UrlActionContext>(
                    a => a.Action == "DeleteArticleAsync" && (string)a.Values.GetType().GetProperty("articleId").GetValue(a.Values, null) == ArticleId)),
                "Delete action url does not include articleId query parameter");
        }

        [TestMethod]
        public async Task UrlToDeleteArticleIncludesAccessToken()
        {
            await _controller.GetNextArticleAsync(AccessToken);

            _mockUrlHelper.Verify(
                u => u.Action(It.Is<UrlActionContext>(
                    a => a.Action == "DeleteArticleAsync" && (string)a.Values.GetType().GetProperty("xAccessToken").GetValue(a.Values, null) == AccessToken)),
                "Delete action url does not include xAccessToken query parameter");
        }

        [TestMethod]
        public async Task FetchingNextArticleReturnsActionsObjectWithUrlToArchiveArticleInResponse()
        {
            var result = await _controller.GetNextArticleAsync(AccessToken);

            Assert.IsInstanceOfType(result, typeof(JsonResult));
            var jsonResult = (JsonResult)result;
            var actions = jsonResult.Value.GetProperty("actions");
            Assert.AreEqual(ArchiveUrl, actions.GetProperty("archive"));
        }

        [TestMethod]
        public async Task UrlToArchiveArticleIncludesArticleId()
        {
            await _controller.GetNextArticleAsync(AccessToken);

            _mockUrlHelper.Verify(
                u => u.Action(It.Is<UrlActionContext>(
                    a => a.Action == "ArchiveArticleAsync" && (string)a.Values.GetType().GetProperty("articleId").GetValue(a.Values, null) == ArticleId)),
                "Archive action url does not include articleId query parameter");
        }

        [TestMethod]
        public async Task UrlToArchiveArticleIncludesAccessToken()
        {
            await _controller.GetNextArticleAsync(AccessToken);

            _mockUrlHelper.Verify(
                u => u.Action(It.Is<UrlActionContext>(
                    a => a.Action == "ArchiveArticleAsync" && (string)a.Values.GetType().GetProperty("xAccessToken").GetValue(a.Values, null) == AccessToken)),
                "Archive action url does not include xAccessToken query parameter");
        }

        [TestMethod]
        public async Task CallsServiceToArchiveArticle()
        {
            await _controller.ArchiveArticleAsync(AccessToken, ArticleId);

            _mockService.Verify(s => s.ArchiveArticleAsync(AccessToken, ArticleId), Times.Exactly(1));
        }

        [TestMethod]
        public async Task ArchivingArticleReturnsForbiddenResponseIfFailsToArchiveArticle()
        {
            _mockService.Setup(s => s.ArchiveArticleAsync(AccessToken, ArticleId)).Throws<PocketException>();

            var result = await _controller.ArchiveArticleAsync(AccessToken, ArticleId);

            Assert.IsInstanceOfType(result, typeof(ObjectResult));
            var objectResult = (ObjectResult)result;
            Assert.AreEqual(403, objectResult.StatusCode);
        }

        [TestMethod]
        public async Task CallsServiceToDeleteArticle()
        {
            await _controller.DeleteArticleAsync(AccessToken, ArticleId);

            _mockService.Verify(s => s.DeleteArticleAsync(AccessToken, ArticleId), Times.Exactly(1));
        }

        [TestMethod]
        public async Task DeleteArticleReturnsForbiddenResponseIfFailsToDeleteArticle()
        {
            _mockService.Setup(s => s.DeleteArticleAsync(AccessToken, ArticleId)).Throws<PocketException>();

            var result = await _controller.DeleteArticleAsync(AccessToken, ArticleId);

            Assert.IsInstanceOfType(result, typeof(ObjectResult));
            var objectResult = (ObjectResult)result;
            Assert.AreEqual(403, objectResult.StatusCode);
        }
    }
}
