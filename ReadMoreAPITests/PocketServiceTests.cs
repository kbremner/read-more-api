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
using ReadMoreAPI;

namespace ReadMoreAPITests
{
    [TestClass]
    public class PocketServiceTests
    {
        private static readonly Uri BaseUri = new Uri("http://base-uri/");
        private static readonly Uri RedirectUri = new Uri("http://redirect-uri/");
        private const string RequestCode = "REQUEST_CODE";
        private static readonly Uri CallbackUrl = new Uri("http://callback_url:80/");
        private static readonly Uri CallerCallbackUrl = new Uri("http://caller-callback-url/");
        private const string AccessToken = "ACCESS_TOKEN";
        private static readonly byte[] AccessTokenBytes = Encoding.UTF8.GetBytes(AccessToken);
        private const string PocketAccessToken = "POCKET_ACCESS_TOKEN";
        private const string PocketUserName = "POCKET_USER_NAME";
        private const string ArticleId = "ARTICLE_ID";
        private static readonly Uri ArticleUrl = new Uri("http://article-url/");
        private const string ArticleTitle = "ARTICLE_TITLE";
        private PocketArticle _article;
        private Guid _accountId;
        private Guid _emailUserId;
        private PocketAccount _account;
        private PocketRequestCode _code;
        private PocketAccessToken _accessTokenResult;
        private Mock<IPocketClient> _mockClient;
        private Mock<IPocketAccountsRepository> _mockRepo;
        private Mock<IDataProtector> _mockDataProtector;
        private Mock<IDataProtectionProvider> _mockDataProtectionProvider;
        private PocketService _service;

        [TestInitialize]
        public void Setup()
        {
            _accountId = Guid.NewGuid();
            _emailUserId = Guid.NewGuid();
            _code = new PocketRequestCode(BaseUri, RedirectUri, RequestCode);
            _accessTokenResult = new PocketAccessToken(PocketUserName, PocketAccessToken);
            _account = new PocketAccount
            {
                Id = _accountId,
                RequestToken = RequestCode,
                AccessToken = PocketAccessToken,
                RedirectUrl = CallerCallbackUrl.ToString()
            };
            _article = new PocketArticle(ArticleId, ArticleUrl, ArticleTitle);


            _mockRepo = new Mock<IPocketAccountsRepository>();
            _mockRepo.Setup(r => r.InsertAsync(It.IsAny<PocketAccount>())).ReturnsAsync((PocketAccount account) => {
                account.Id = _accountId;
                return account;
            });
            _mockRepo.Setup(r => r.FindByIdAsync(_accountId)).ReturnsAsync(_account);
            
            _mockClient = new Mock<IPocketClient>();
            _mockClient.Setup(c => c.CreateRequestCodeAsync(It.IsAny<Uri>())).ReturnsAsync(_code);
            _mockClient.Setup(c => c.CreateAccessTokenAsync(RequestCode)).ReturnsAsync(_accessTokenResult);
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

            _mockRepo.Verify(r => r.InsertAsync(It.Is<PocketAccount>(a => a.RedirectUrl == CallerCallbackUrl.ToString())), Times.Exactly(1));
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
            var expectedAccessToken = _mockDataProtector.Object.Protect(AccessToken);
            var uriBuilder = new UriBuilder(CallbackUrl);
            uriBuilder.AppendToQuery("xAccessToken", expectedAccessToken);

            await _service.GenerateAuthUrlAsync(CallbackUrl, CallerCallbackUrl);

            _mockClient.Verify(c => c.CreateRequestCodeAsync(uriBuilder.Uri), Times.Exactly(1));
        }

        [TestMethod]
        public async Task GenerateAuthUrlHandlesExistingQueryParametersInCallbackUrl()
        {
            var expectedAccessToken = _mockDataProtector.Object.Protect(AccessToken);
            var uriBuilder = new UriBuilder(CallbackUrl);
            uriBuilder.AppendToQuery("a", "b");

            await _service.GenerateAuthUrlAsync(uriBuilder.Uri, CallerCallbackUrl);

            uriBuilder.AppendToQuery("xAccessToken", expectedAccessToken);
            _mockClient.Verify(c => c.CreateRequestCodeAsync(uriBuilder.Uri), Times.Exactly(1));
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
            await _service.UpgradeRequestTokenAsync(AccessToken);

            _mockRepo.Verify(r => r.FindByIdAsync(_accountId), Times.Exactly(1));
        }

        [TestMethod]
        public async Task CreatesPocketAccessTokenUsingRequestCodeAssociatedWithBackendAccessToken()
        {
            await _service.UpgradeRequestTokenAsync(AccessToken);

            _mockClient.Verify(c => c.CreateAccessTokenAsync(RequestCode), Times.Exactly(1));
        }

        [TestMethod]
        public async Task ReturnedUriIncludesProvidedAccessToken()
        {
            var uriBuilder = new UriBuilder(CallerCallbackUrl);
            uriBuilder.AppendToQuery("xAccessToken", AccessToken);
            
            var result = await _service.UpgradeRequestTokenAsync(AccessToken);

            Assert.AreEqual(uriBuilder.Uri, result);
        }

        [TestMethod]
        public async Task UpdatesStoredPocketAccountWithCreatedPocketAccessToken()
        {
            await _service.UpgradeRequestTokenAsync(AccessToken);

            _mockRepo.Verify(r => r.UpdateAsync(It.Is<PocketAccount>(a => a.AccessToken == PocketAccessToken)));
        }

        [TestMethod]
        public async Task ClearsRequestCodeInStoredPocketAccount()
        {
            await _service.UpgradeRequestTokenAsync(AccessToken);

            _mockRepo.Verify(r => r.UpdateAsync(It.Is<PocketAccount>(a => a.RequestToken == null)));
        }

        [TestMethod]
        public async Task DeletesAccountIfClientThrowsExceptionWhenCreatingPocketAccessToken()
        {
            _mockClient.Setup(c => c.CreateAccessTokenAsync(RequestCode)).Throws<PocketException>();

            await _service.UpgradeRequestTokenAsync(AccessToken);

            _mockRepo.Verify(r => r.DeleteAsync(_account), Times.Exactly(1));
        }

        [TestMethod]
        public async Task ReturnedUriIncludesErrorIfClientThrowsExceptionWhenCreatingPocketAccessToken()
        {
            var uriBuilder = new UriBuilder(CallerCallbackUrl);
            uriBuilder.AppendToQuery("error", "auth_failed");
            _mockClient.Setup(c => c.CreateAccessTokenAsync(RequestCode)).Throws<PocketException>();

            var result = await _service.UpgradeRequestTokenAsync(AccessToken);

            Assert.AreEqual(uriBuilder.Uri, result);
        }

        [TestMethod]
        public async Task DeletesAccountIfClientThrowsExceptionWhenCreatingRequestCode()
        {
            _mockClient.Setup(c => c.CreateRequestCodeAsync(It.IsAny<Uri>())).Throws<PocketException>();

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

        [TestMethod]
        public async Task DeletesNewAccountIfAccountAlreadyExistsForUsername()
        {
            var existingAccount = new PocketAccount();
            _mockRepo.Setup(r => r.FindByUsernameAsync(PocketUserName)).ReturnsAsync(existingAccount);

            await _service.UpgradeRequestTokenAsync(AccessToken);

            _mockRepo.Verify(r => r.DeleteAsync(_account), Times.Exactly(1));
        }

        [TestMethod]
        public async Task UpdatesAccessTokenForExistingAccount()
        {
            var existingAccount = new PocketAccount();
            _mockRepo.Setup(r => r.FindByUsernameAsync(PocketUserName)).ReturnsAsync(existingAccount);

            await _service.UpgradeRequestTokenAsync(AccessToken);

            Assert.AreEqual(PocketAccessToken, existingAccount.AccessToken);
        }

        [TestMethod]
        public async Task UsesProtectedIdOfExistingAccountInUrl()
        {
            var existingAccount = new PocketAccount { Id = Guid.NewGuid() };
            var existingTokenBytes = Encoding.UTF8.GetBytes(existingAccount.Id.ToString());
            _mockDataProtector.Setup(p => p.Protect(It.IsAny<byte[]>())).Returns(existingTokenBytes);
            var expectedToken = _mockDataProtector.Object.Protect(existingAccount.Id.ToString());
            var uriBuilder = new UriBuilder(CallerCallbackUrl);
            uriBuilder.AppendToQuery("xAccessToken", expectedToken);
            _mockRepo.Setup(r => r.FindByUsernameAsync(PocketUserName)).ReturnsAsync(existingAccount);

            var result = await _service.UpgradeRequestTokenAsync(AccessToken);

            Assert.AreEqual(uriBuilder.Uri, result);
        }

        [TestMethod]
        public async Task UpdatesRedirectUrlForExistingAccount()
        {
            var existingAccount = new PocketAccount();
            _mockRepo.Setup(r => r.FindByUsernameAsync(PocketUserName)).ReturnsAsync(existingAccount);

            await _service.UpgradeRequestTokenAsync(AccessToken);

            Assert.AreEqual(CallerCallbackUrl, existingAccount.RedirectUrl);
        }

        [TestMethod]
        public async Task ReturnsTogglesFromRepo()
        {
            var toggle = new FeatureToggle
            {
                Id = Guid.NewGuid(),
                Name = "Test Toggle",
                Description = "Test Feature Toggle Description"
            };
            var toggles = new[] {toggle};
            _mockRepo.Setup(r => r.FindTogglesForAccountAsync(_accountId)).ReturnsAsync(toggles);

            var result = await _service.GetFeatureTogglesAsync(AccessToken);

            CollectionAssert.AreEqual(toggles, result.ToList());
        }
    }
}
