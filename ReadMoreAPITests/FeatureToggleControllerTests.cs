using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PocketLib;
using ReadMoreAPI.Controllers;
using ReadMoreAPI.Services;
using ReadMoreData.Models;

namespace ReadMoreAPITests
{
    [TestClass]
    public class FeatureToggleControllerTests
    {
        private const string AccessToken = "ACCESS_TOKEN";
        private Mock<IPocketService> _mockService;
        private FeatureToggleController _controller;
        private FeatureToggle _toggle;
        private FeatureToggle[] _toggles;

        [TestInitialize]
        public void Setup()
        {
            _toggle = new FeatureToggle
            {
                Id = Guid.NewGuid(),
                Name = "Test Toggle",
                Description = "Test Toggle Description"
            };
            _toggles = new[] {_toggle};

            _mockService = new Mock<IPocketService>();
            _mockService.Setup(s => s.GetFeatureTogglesAsync(AccessToken)).ReturnsAsync(_toggles);

            _controller = new FeatureToggleController(Mock.Of<ILogger<FeatureToggleController>>(), _mockService.Object);
        }

        [TestMethod]
        public async Task ReturnsTogglesFromService()
        {
            var result = await _controller.GetTogglesForUser(AccessToken);

            Assert.IsInstanceOfType(result, typeof(JsonResult));
            var jsonResult = (JsonResult)result;
            Assert.AreEqual(_toggles, jsonResult.Value);
        }

        [TestMethod]
        public async Task ReturnsForbiddenResponseIfExceptionThrownWhenGettingToggles()
        {
            _mockService.Setup(s => s.GetFeatureTogglesAsync(AccessToken)).Throws<PocketException>();

            var result = await _controller.GetTogglesForUser(AccessToken);

            Assert.IsInstanceOfType(result, typeof(ObjectResult));
            var objectResult = (ObjectResult)result;
            Assert.AreEqual(403, objectResult.StatusCode);
        }
    }
}
