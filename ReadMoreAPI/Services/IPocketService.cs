using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using PocketLib;
using ReadMoreData.Models;

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
        Task<IEnumerable<FeatureToggle>> GetFeatureTogglesAsync(string xAccessToken);
    }
}
