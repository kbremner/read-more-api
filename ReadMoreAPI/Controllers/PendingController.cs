using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PocketLib;
using ReadMoreAPI.Services;

namespace ReadMoreAPI.Controllers
{
    [Produces("application/json")]
    [Route("api/pending")]
    public class PendingController : Controller
    {
        private readonly ILogger _logger;
        private readonly IPocketService _service;

        // ReSharper disable once SuggestBaseTypeForParameter
        // The ILogger needs to be parameterized otherwise it is not resolved
        public PendingController(ILogger<PendingController> logger, IPocketService service)
        {
            _logger = logger;
            _service = service;
        }

        [HttpGet("email")]
        [RequiredQueryParameter(Name = "xAccessToken")]
        public async Task<IActionResult> GetBacklogEmailAddress(string xAccessToken)
        {
            try
            {
                var email = await _service.GetBacklogEmailAddressAsync(xAccessToken);
                return Json(new {email});
            }
            catch (PocketException e)
            {
                _logger.LogError(e, "Failed to get backlog email address");
                return StatusCode((int)HttpStatusCode.Forbidden, new { error = "Failed to get backlog email address" });
            }
        }
    }
}