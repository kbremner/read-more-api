using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PocketLib;
using ReadMoreAPI.Controllers;
using ReadMoreAPI.Services;

namespace ReadMoreAPITests
{
    [TestClass]
    public class PendingControllerTests
    {
        private const string AccessToken = "ACCESS_TOKEN";
        private const string Email = "some@email.address";
        private Mock<IPocketService> _mockService;
        private PendingController _controller;

        [TestInitialize]
        public void Setup()
        {
            _mockService = new Mock<IPocketService>();
            _mockService.Setup(s => s.GetBacklogEmailAddressAsync(AccessToken)).ReturnsAsync(Email);

            _controller = new PendingController(Mock.Of<ILogger<PendingController>>(), _mockService.Object);
        }

        [TestMethod]
        public async Task ReturnsEmailFromService()
        {
            var result = await _controller.GetBacklogEmailAddress(AccessToken);

            Assert.IsInstanceOfType(result, typeof(JsonResult));
            var jsonResult = (JsonResult)result;
            Assert.AreEqual(Email, jsonResult.Value.GetProperty("email"));
        }

        [TestMethod]
        public async Task ReturnsForbiddenResponseIfExceptionThrownWhenGettingEmail()
        {
            _mockService.Setup(s => s.GetBacklogEmailAddressAsync(AccessToken)).Throws<PocketException>();

            var result = await _controller.GetBacklogEmailAddress(AccessToken);

            Assert.IsInstanceOfType(result, typeof(ObjectResult));
            var objectResult = (ObjectResult)result;
            Assert.AreEqual(403, objectResult.StatusCode);
        }
    }
}
