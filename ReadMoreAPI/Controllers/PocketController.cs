using System;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PocketLib;
using ReadMoreAPI.Services;
using ReadMoreData.Models;
using System.Threading.Tasks;

namespace ReadMoreAPI.Controllers
{
    [Produces("application/json")]
    [Route("api/pocket")]
    public class PocketController : Controller
    {
        private readonly ILogger _logger;
        private readonly IPocketService _service;

        // ReSharper disable once SuggestBaseTypeForParameter
        // The ILogger needs to be parameterized otherwise it is not resolved
        public PocketController(ILogger<PocketController> logger, IPocketService service)
        {
            _logger = logger;
            _service = service;
        }

        [HttpGet("authorize")]
        [RequiredQueryParameter(Name = "redirectUrl", ActionParameterName = "callerRedirectUrl")]
        public async Task<ActionResult> Authorize(string callerRedirectUrl)
        {
            var callbackUrl = Url.Action("CompleteAuth", null, null, Request.Scheme);

            var pocketAuthUrl = await _service.GenerateAuthUrlAsync(callbackUrl, callerRedirectUrl);

            return Redirect(pocketAuthUrl);
        }

        [HttpGet("callback")]
        [RequiredQueryParameter(Name = "xAccessToken")]
        public async Task<IActionResult> CompleteAuth(string xAccessToken)
        {
            // Upgrade the request token and redirect to the caller's provided redirect URL
            var uri = await _service.UpgradeRequestTokenAsync(xAccessToken);
            return Redirect(uri.ToString());
        }

        [HttpGet("next")]
        [RequiredQueryParameter(Name = "xAccessToken")]
        public async Task<IActionResult> GetNextArticleAsync(string xAccessToken)
        {
            PocketArticle article;
            try
            {
                article = await _service.GetNextArticleAsync(xAccessToken);
            }
            catch (PocketException e)
            {
                _logger.LogError(e, "Failed to get next article from pocket");
                return StatusCode((int)HttpStatusCode.Forbidden, new { error = "Failed to get next article from pocket" });
            }

            return Json(new
            {
                url = article.Url,
                actions = new
                {
                    delete = Url.Action("DeleteArticleAsync", null,
                        new { articleId = article.ItemId, xAccessToken }, Request.Scheme),
                    archive = Url.Action("ArchiveArticleAsync", null,
                        new { articleId = article.ItemId, xAccessToken }, Request.Scheme)
                }
            });
        }

        [HttpGet("delete")]
        [RequiredQueryParameter(Name = "xAccessToken")]
        [RequiredQueryParameter(Name = "articleId")]
        public async Task<IActionResult> DeleteArticleAsync(string xAccessToken, string articleId)
        {
            try
            {
                await _service.DeleteArticleAsync(xAccessToken, articleId);
            }
            catch (PocketException e)
            {
                _logger.LogError(e, "Failed to delete pocket article");
                return StatusCode((int)HttpStatusCode.Forbidden, new { error = "Failed to delete pocket article" });
            }

            return NoContent();
        }

        [HttpGet("archive")]
        [RequiredQueryParameter(Name = "xAccessToken")]
        [RequiredQueryParameter(Name = "articleId")]
        public async Task<IActionResult> ArchiveArticleAsync(string xAccessToken, string articleId)
        {
            try
            {
                await _service.ArchiveArticleAsync(xAccessToken, articleId);
            }
            catch (PocketException e)
            {
                _logger.LogError(e, "Failed to archive pocket article");
                return StatusCode((int)HttpStatusCode.Forbidden, new { error = "Failed to archive pocket article" });
            }

            return NoContent();
        }
    }
}