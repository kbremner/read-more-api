using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PocketLib;
using ReadMoreAPI.Services;
using ReadMoreData.Models;

namespace ReadMoreAPI.Controllers
{
    [Produces("application/json")]
    [Route("api/toggles")]
    public class FeatureToggleController : Controller
    {
        private readonly ILogger _logger;
        private readonly IPocketService _service;

        // ReSharper disable once SuggestBaseTypeForParameter
        // The ILogger needs to be parameterized otherwise it is not resolved
        public FeatureToggleController(ILogger<FeatureToggleController> logger, IPocketService service)
        {
            _logger = logger;
            _service = service;
        }

        [HttpGet]
        [RequiredQueryParameter(Name = "xAccessToken")]
        public async Task<IActionResult> GetTogglesForUser(string xAccessToken)
        {
            try {
                var toggles = await _service.GetFeatureTogglesAsync(xAccessToken);
                return new JsonResult(toggles);
            }
            catch (PocketException e)
            {
                _logger.LogError(e, "Failed to get toggles");
                return StatusCode((int) HttpStatusCode.Forbidden, new { error = "Failed to get toggles" });
            }
        }
    }
}