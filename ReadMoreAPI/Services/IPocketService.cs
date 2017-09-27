using System;
using System.Threading.Tasks;
using PocketLib;

namespace ReadMoreAPI.Services
{
    public interface IPocketService
    {
        Task<string> GenerateAuthUrlAsync(string oauthCallbackUrl, string callerRedirectUrl);
        Task<Uri> UpgradeRequestTokenAsync(string accountAccessToken);
        Task<PocketArticle> GetNextArticleAsync(string accessToken);
        Task DeleteArticleAsync(string accountAccessToken, string articleId);
        Task ArchiveArticleAsync(string accountAccessToken, string articleId);
    }
}
