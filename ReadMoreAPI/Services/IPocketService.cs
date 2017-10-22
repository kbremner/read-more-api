using System;
using System.Threading.Tasks;
using PocketLib;

namespace ReadMoreAPI.Services
{
    public interface IPocketService
    {
        Task<Uri> GenerateAuthUrlAsync(Uri oauthCallbackUrl, Uri callerRedirectUrl);
        Task<Uri> UpgradeRequestTokenAsync(string accountAccessToken);
        Task<PocketArticle> GetNextArticleAsync(string accessToken);
        Task DeleteArticleAsync(string accountAccessToken, string articleId);
        Task ArchiveArticleAsync(string accountAccessToken, string articleId);
        Task<string> GetBacklogEmailAddressAsync(string xAccessToken);
    }
}
