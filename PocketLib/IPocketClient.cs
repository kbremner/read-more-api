using System;
using System.Threading.Tasks;

namespace PocketLib
{
    public interface IPocketClient
    {
        Task<PocketRequestCode> CreateRequestCodeAsync(Uri redirectUri);
        Task<string> CreateAccessTokenAsync(string requestCode);
        Task<PocketArticle> GetRandomArticleAsync(string accessToken, int countToRetrieve = 200);
        Task DeleteArticleAsync(string accessToken, string articleId);
        Task ArchiveArticleAsync(string accessToken, string articleId);
    }
}