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

        public async Task<Uri> GenerateAuthUrlAsync(Uri oauthCallbackUrl, Uri callerRedirectUrl)
        {
            // Pocket will redirect back to this app after asking the user to grant us access.
            // So that we can later retrieve the created request code and convert it in to a
            // pocket access token, we need to include a reference in the redirect URL. The
            // encrypted ID of an account is used as our access token meaning that, as the
            // redirect URL is required to get a request code, we have to create the account
            // here first before proceeding.
            var entry = await _repo.InsertAsync(new PocketAccount
            {
                RedirectUrl = callerRedirectUrl.ToString()
            });

            // add encrypted ID to URL as query parameter
            var protectedAccessToken = _protector.Protect(entry.Id.ToString());
            var uriBuilder = new UriBuilder(oauthCallbackUrl);
            uriBuilder.AppendToQuery("xAccessToken", protectedAccessToken);

            // Can now get a request token to start the process
            PocketRequestCode requestCode;
            try
            {
                requestCode = await _client.CreateRequestCodeAsync(uriBuilder.Uri);
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

        public async Task<Uri> UpgradeRequestTokenAsync(string accountAccessToken)
        {
            // retrieve PocketAccount for access token
            var id = _protector.Unprotect(accountAccessToken);
            var account = await _repo.FindByIdAsync(new Guid(id));
            var uriBuilder = new UriBuilder(account.RedirectUrl);

            try
            {
                // convert request token from PocketAccount in to pocket access token
                var accessTokenResult = await _client.CreateAccessTokenAsync(account.RequestToken);

                // we've got a username now, so check if an account already exists with that username
                var usernameAccount = await _repo.FindByUsernameAsync(accessTokenResult.Username);
                if (usernameAccount != null)
                {
                    // user has already registered, so update their account with the new access token
                    usernameAccount.AccessToken = accessTokenResult.AccessToken;
                    await _repo.UpdateAsync(usernameAccount);

                    // and delete the temporary account
                    await _repo.DeleteAsync(account);

                    // update the account access token that will be added to the url
                    accountAccessToken = _protector.Protect(usernameAccount.Id.ToString());
                }
                else
                {
                    // user hasn't signed in before, so update the temporary account with details
                    account.AccessToken = accessTokenResult.AccessToken;
                    account.Username = accessTokenResult.Username;
                    account.RequestToken = null;

                    // and save it
                    await _repo.UpdateAsync(account);
                }
                
                // add the access token to the redirect url
                uriBuilder.AppendToQuery("xAccessToken", accountAccessToken);
            }
            catch (PocketException)
            {
                // authentication failed, clean up by deleting the PocketAccount
                await _repo.DeleteAsync(account);
                // add an error query paramter to the redirect url
                uriBuilder.AppendToQuery("error", "auth_failed");
            }

            // return the updated provided 
            return uriBuilder.Uri;
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
    }
}
