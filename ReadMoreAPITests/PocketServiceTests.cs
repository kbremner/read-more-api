using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PocketLib;
using ReadMoreAPI.Services;
using ReadMoreData;
using ReadMoreData.Models;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReadMoreAPITests
{
    [TestClass]
    public class PocketServiceTests
    {
        private const string BaseUri = "BASE_URI";
        private const string RedirectUri = "REDIRECT_URI";
        private const string RequestCode = "REQUEST_CODE";
        private const string CallbackUrl = "http://callback_url:80/";
        private const string CallerCallbackUrl = "CALLER_CALLBACK_URL";
        private const string AccessToken = "ACCESS_TOKEN";
        private static readonly byte[] AccessTokenBytes = Encoding.UTF8.GetBytes(AccessToken);
        private static readonly string ProtectedAccessToken = WebEncoders.Base64UrlEncode(AccessTokenBytes);
        private const string PocketAccessToken = "POCKET_ACCESS_TOKEN";
        private const string ArticleId = "ARTICLE_ID";
        private const string ArticleUrl = "ARTICLE_URL";
        private const string ArticleTitle = "ARTICLE_TITLE";
        private PocketArticle _article;
        private Guid _accountId;
        private PocketAccount _account;
        private PocketRequestCode _code;
        private Mock<IPocketClient> _mockClient;
        private Mock<IPocketAccountsRepository> _mockRepo;
        private Mock<IDataProtector> _mockDataProtector;
        private Mock<IDataProtectionProvider> _mockDataProtectionProvider;
        private PocketService _service;

        [TestInitialize]
        public void Setup()
        {
            _accountId = Guid.NewGuid();
            _code = new PocketRequestCode(BaseUri, RedirectUri, RequestCode);
            _account = new PocketAccount
            {
                Id = _accountId,
                RequestToken = RequestCode,
                AccessToken = PocketAccessToken
            };
            _article = new PocketArticle(ArticleId, ArticleUrl, ArticleTitle);


            _mockRepo = new Mock<IPocketAccountsRepository>();
            _mockRepo.Setup(r => r.InsertAsync(It.IsAny<PocketAccount>())).ReturnsAsync((PocketAccount account) => {
                account.Id = _accountId;
                return account;
            });
            _mockRepo.Setup(r => r.FindByIdAsync(_accountId)).ReturnsAsync(_account);
            
            _mockClient = new Mock<IPocketClient>();
            _mockClient.Setup(c => c.CreateRequestCodeAsync(It.IsAny<string>())).ReturnsAsync(_code);
            _mockClient.Setup(c => c.CreateAccessTokenAsync(RequestCode)).ReturnsAsync(PocketAccessToken);
            _mockClient.Setup(c => c.GetRandomArticleAsync(PocketAccessToken, 200)).ReturnsAsync(_article);

            _mockDataProtector = new Mock<IDataProtector>();
            _mockDataProtector.Setup(p => p.Protect(It.IsAny<byte[]>())).Returns(AccessTokenBytes);
            _mockDataProtector.Setup(p => p.Unprotect(It.IsAny<byte[]>())).Returns(Encoding.UTF8.GetBytes(_accountId.ToString()));

            _mockDataProtectionProvider = new Mock<IDataProtectionProvider>();
            _mockDataProtectionProvider.Setup(p => p.CreateProtector(It.IsAny<string>())).Returns(_mockDataProtector.Object);

            _service = new PocketService(_mockRepo.Object, _mockClient.Object, _mockDataProtectionProvider.Object);
        }

        [TestMethod]
        public void CreatesProtectorToProtectAccessToken()
        {
            _mockDataProtectionProvider.Verify(p => p.CreateProtector("access-token-protector"), Times.Exactly(1));
        }

        [TestMethod]
        public async Task GenerateAuthUrlCreatesPocketAccountWithCallerRedirectUrl()
        {
            await _service.GenerateAuthUrlAsync(CallbackUrl, CallerCallbackUrl);

            _mockRepo.Verify(r => r.InsertAsync(It.Is<PocketAccount>(a => a.RedirectUrl == CallerCallbackUrl)), Times.Exactly(1));
        }

        [TestMethod]
        public async Task GenerateAuthUrlProtectsAccountId()
        {
            await _service.GenerateAuthUrlAsync(CallbackUrl, CallerCallbackUrl);

            _mockDataProtector.Verify(p => p.Protect(It.Is<byte[]>(
                b => b.SequenceEqual(Encoding.UTF8.GetBytes(_accountId.ToString())))), Times.Exactly(1));
        }

        [TestMethod]
        public async Task GenerateAuthUrlGetsRequestTokenUsingCallbackUrlWithProtectedAccessToken()
        {
            var expectedUri = $"{CallbackUrl}?xAccessToken={ProtectedAccessToken}";

            await _service.GenerateAuthUrlAsync(CallbackUrl, CallerCallbackUrl);

            _mockClient.Verify(c => c.CreateRequestCodeAsync(expectedUri), Times.Exactly(1));
        }

        [TestMethod]
        public async Task GenerateAuthUrlHandlesExistingQueryParametersInCallbackUrl()
        {
            const string callbackUrl = CallbackUrl + "?a=b";
            var accessTokenBytes = Encoding.UTF8.GetBytes(AccessToken);
            var expectedProtectedValue = WebEncoders.Base64UrlEncode(accessTokenBytes);

            _mockDataProtector.Setup(p => p.Protect(It.IsAny<byte[]>())).Returns(accessTokenBytes);
            var expectedUri = $"{callbackUrl}&xAccessToken={expectedProtectedValue}";

            await _service.GenerateAuthUrlAsync(callbackUrl, CallerCallbackUrl);

            _mockClient.Verify(c => c.CreateRequestCodeAsync(expectedUri), Times.Exactly(1));
        }

        [TestMethod]
        public async Task GenerateAuthUrlUpdatesPocketAccountWithRequestCode()
        {
            await _service.GenerateAuthUrlAsync(CallbackUrl, CallerCallbackUrl);

            _mockRepo.Verify(r => r.UpdateAsync(It.Is<PocketAccount>(a => a.Id == _accountId && a.RequestToken == RequestCode)), Times.Exactly(1));
        }

        [TestMethod]
        public async Task GenerateAuthUrlReturnsUrlFromRequestCode()
        {
            var result = await _service.GenerateAuthUrlAsync(CallbackUrl, CallerCallbackUrl);

            Assert.AreEqual(_code.ToAuthUrl(), result);
        }

        [TestMethod]
        public async Task GetsPocketAccountAssociatedWithAccessTokenWhenCreatingPocketAccessToken()
        {
            await _service.CreatePocketAccessTokenForAccountAsync(AccessToken);

            _mockRepo.Verify(r => r.FindByIdAsync(_accountId), Times.Exactly(1));
        }

        [TestMethod]
        public async Task CreatesPocketAccessTokenUsingRequestCodeAssociatedWithBackendAccessToken()
        {
            await _service.CreatePocketAccessTokenForAccountAsync(AccessToken);

            _mockClient.Verify(c => c.CreateAccessTokenAsync(RequestCode), Times.Exactly(1));
        }

        [TestMethod]
        public async Task ReturnedPocketAccountIncludesCreatedPocketAccessToken()
        {
            var result = await _service.CreatePocketAccessTokenForAccountAsync(AccessToken);

            Assert.AreEqual(PocketAccessToken, result.AccessToken);
        }

        [TestMethod]
        public async Task ClearsRequestCodeInReturnedPocketAccount()
        {
            var result = await _service.CreatePocketAccessTokenForAccountAsync(AccessToken);

            Assert.IsNull(result.RequestToken);
        }

        [TestMethod]
        public async Task UpdatesStoredPocketAccountWithCreatedPocketAccessToken()
        {
            await _service.CreatePocketAccessTokenForAccountAsync(AccessToken);

            _mockRepo.Verify(r => r.UpdateAsync(It.Is<PocketAccount>(a => a.AccessToken == PocketAccessToken)));
        }

        [TestMethod]
        public async Task ClearsRequestCodeInStoredPocketAccount()
        {
            await _service.CreatePocketAccessTokenForAccountAsync(AccessToken);

            _mockRepo.Verify(r => r.UpdateAsync(It.Is<PocketAccount>(a => a.RequestToken == null)));
        }

        [TestMethod]
        public async Task DeletesAccountIfClientThrowsExceptionWhenCreatingPocketAccessToken()
        {
            _mockClient.Setup(c => c.CreateAccessTokenAsync(RequestCode)).Throws<PocketException>();

            try
            {
                await _service.CreatePocketAccessTokenForAccountAsync(AccessToken);
            }
            catch (PocketException)
            {
                // no-op
            }

            _mockRepo.Verify(r => r.DeleteAsync(_account), Times.Exactly(1));
        }

        [TestMethod]
        public async Task DeletesAccountIfClientThrowsExceptionWhenCreatingRequestCode()
        {
            _mockClient.Setup(c => c.CreateRequestCodeAsync(It.IsAny<string>())).Throws<PocketException>();

            try
            {
                await _service.GenerateAuthUrlAsync(CallbackUrl, CallerCallbackUrl);
            }
            catch (PocketException)
            {
                // no-op
            }

            _mockRepo.Verify(r => r.DeleteAsync(It.Is<PocketAccount>(a => a.Id == _accountId)), Times.Exactly(1));
        }

        [TestMethod]
        public void ProtectsAccountIdBeforeAppendingToUrl()
        {
            _service.AppendAccessTokenToUrl(_account, CallerCallbackUrl);

            _mockDataProtector.Verify(p => p.Protect(It.Is<byte[]>(
                b => b.SequenceEqual(Encoding.UTF8.GetBytes(_accountId.ToString())))), Times.Exactly(1));
        }

        [TestMethod]
        public async Task GetsPocketAccountAssociatedWithAccessTokenWhenGettingNextArticle()
        {
            await _service.GetNextArticleAsync(AccessToken);

            _mockRepo.Verify(r => r.FindByIdAsync(_accountId), Times.Exactly(1));
        }

        [TestMethod]
        public async Task ReturnsRandomArticleFromClientWhenGettingNextArticle()
        {
            var result = await _service.GetNextArticleAsync(AccessToken);

            Assert.AreEqual(_article, result);
        }

        [TestMethod]
        public async Task GetsPocketAccountAssociatedWithAccessTokenWhenDeletingArticle()
        {
            await _service.DeleteArticleAsync(AccessToken, ArticleId);

            _mockRepo.Verify(r => r.FindByIdAsync(_accountId), Times.Exactly(1));
        }

        [TestMethod]
        public async Task CallsClientToDeleteArticleWhenDeletingArticle()
        {
            await _service.DeleteArticleAsync(AccessToken, ArticleId);

            _mockClient.Verify(c => c.DeleteArticleAsync(_account.AccessToken, ArticleId), Times.Exactly(1));
        }

        [TestMethod]
        public async Task GetsPocketAccountAssociatedWithAccessTokenWhenArchivingArticle()
        {
            await _service.ArchiveArticleAsync(AccessToken, ArticleId);

            _mockRepo.Verify(r => r.FindByIdAsync(_accountId), Times.Exactly(1));
        }

        [TestMethod]
        public async Task CallsClientToArchiveArticleWhenArchivingArticle()
        {
            await _service.ArchiveArticleAsync(AccessToken, ArticleId);

            _mockClient.Verify(c => c.ArchiveArticleAsync(_account.AccessToken, ArticleId), Times.Exactly(1));
        }
    }
}
