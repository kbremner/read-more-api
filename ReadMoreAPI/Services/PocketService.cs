using Microsoft.AspNetCore.DataProtection;
using PocketLib;
using ReadMoreData;
using ReadMoreData.Models;
using System;
using System.Threading.Tasks;

namespace ReadMoreAPI.Services
{
    public class PocketService : IPocketService
    {
        private readonly IPocketAccountsRepository _repo;
        private readonly IPocketClient _client;
        private readonly IDataProtector _protector;

        public PocketService(IPocketAccountsRepository repo, IPocketClient client, IDataProtectionProvider protectionProvider)
        {
            _repo = repo;
            _client = client;
            _protector = protectionProvider.CreateProtector("access-token-protector");
        }

        public async Task<string> GenerateAuthUrlAsync(string oauthCallbackUrl, string callerRedirectUrl)
        {
            // Pocket will redirect back to this app after asking the user to grant us access.
            // So that we can later retrieve the created request code and convert it in to a
            // pocket access token, we need to include a reference in the redirect URL. The
            // encrypted ID of an account is used as our access token meaning that, as the
            // redirect URL is required to get a request code, we have to create the account
            // here first before proceeding.
            var entry = await _repo.InsertAsync(new PocketAccount
            {
                RedirectUrl = callerRedirectUrl
            });

            // add encrypted ID to URL as query parameter
            var uri = AppendAccessTokenToUrl(entry, oauthCallbackUrl);

            // Can now get a request token to start the process
            PocketRequestCode requestCode;
            try
            {
                requestCode = await _client.CreateRequestCodeAsync(uri);
            }
            catch (PocketException)
            {
                await _repo.DeleteAsync(entry);
                throw;
            }

            // Persist the request token so that it's available when we get redirected back
            entry.RequestToken = requestCode.Code;
            await _repo.UpdateAsync(entry);

            return requestCode.ToAuthUrl();
        }

        public async Task<PocketAccount> CreatePocketAccessTokenForAccountAsync(string accountAccessToken)
        {
            // 1 - retrieve PocketAccount for access token
            var id = _protector.Unprotect(accountAccessToken);
            var account = await _repo.FindByIdAsync(new Guid(id));

            // 2 - convert request token from PocketAccount in to pocket access token
            string pocketAccessToken;
            try
            {
                pocketAccessToken = await _client.CreateAccessTokenAsync(account.RequestToken);
            }
            catch (PocketException)
            {
                // authentication failed, clean up by deleting the PocketAccount
                await _repo.DeleteAsync(account);
                throw;
            }

            // 3 - update PocketAccount with pocket access token
            account.AccessToken = pocketAccessToken;
            account.RequestToken = null;
            await _repo.UpdateAsync(account);

            return account;
        }

        public async Task<PocketArticle> GetNextArticleAsync(string accountAccessToken)
        {
            var id = _protector.Unprotect(accountAccessToken);
            var account = await _repo.FindByIdAsync(new Guid(id));
            
            return await _client.GetRandomArticleAsync(account.AccessToken);
        }

        public async Task DeleteArticleAsync(string accountAccessToken, string articleId)
        {
            var id = _protector.Unprotect(accountAccessToken);
            var account = await _repo.FindByIdAsync(new Guid(id));

            await _client.DeleteArticleAsync(account.AccessToken, articleId);
        }

        public async Task ArchiveArticleAsync(string accountAccessToken, string articleId)
        {
            var id = _protector.Unprotect(accountAccessToken);
            var account = await _repo.FindByIdAsync(new Guid(id));

            await _client.ArchiveArticleAsync(account.AccessToken, articleId);
        }

        public string AppendAccessTokenToUrl(PocketAccount account, string url)
        {
            var protectedAccessToken = _protector.Protect(account.Id.ToString());
            var uri = new UriBuilder(url);
            if (string.IsNullOrWhiteSpace(uri.Query))
            {
                uri.Query = $"xAccessToken={protectedAccessToken}";
            }
            else
            {
                uri.Query += $"&xAccessToken={protectedAccessToken}";
            }
            return uri.ToString();
        }
    }
}
