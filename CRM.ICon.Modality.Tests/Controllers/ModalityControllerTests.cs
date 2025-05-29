using CRM.ICon.Modality.Controllers;
using CRM.ICon.Modality.Helpers.Cosmos;
using CRM.ICon.Modality.Helpers.Telemetry;
using CRM.ICon.Modality.Model;
using CRM.ICon.Modality.Services.Omnichannel;
using CRM.ICon.Modality.Services.VDM;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace CRM.ICon.Modality.Tests.Controllers
{
    public class ModalityControllerTests
    {
        private ModalityController CreateController()
        {
            var vdmService = Substitute.For<IVDMService>();
            var omnichannelService = Substitute.For<IOmnichannelService>();
            var telemetryService = Substitute.For<ITelemetryService>();
            var omniChannelEUService = Substitute.For<IOmnichannelEUService>();
            var cosmosDbClient = Substitute.For<ICosmosDbClient>();
            return new ModalityController(vdmService, omnichannelService, telemetryService, omniChannelEUService, cosmosDbClient);
        }

        [Theory]
        [InlineData("GetAvailableModalities")]
        [InlineData("GetWidgetDetails")]
        [InlineData("CreateThemeSubjectMapping")]
        public void AllMethods_ShouldHaveAuthorizeAttribute(string methodName)
        {
            var controller = CreateController();
            var type = controller.GetType();
            var methodInfo = type.GetMethod(methodName);
            var attributes = methodInfo.GetCustomAttributes(typeof(AuthorizeAttribute), true);

            Assert.True(attributes.Any()); // Ensure [Authorize] Attribute exists
        }

        [Fact]
        public async Task GetAvailableModalities_ShouldReturnBadRequest_WhenRequestIsNull()
        {
            var controller = CreateController();

            var result = await controller.GetAvailableModalities(null, new HeaderDictionary());

            var objectResult = result.Result as ObjectResult;
            Assert.NotNull(objectResult);
            Assert.Equal(400, objectResult.StatusCode);
            // Optionally check the message:
            // Assert.Equal("Request body for GetAvailableModalities is null", objectResult.Value);
        }

        [Fact]
        public async Task GetAvailableModalities_ShouldReturnOk_WhenRequestIsValid()
        {
            var controller = CreateController();

            var modalityRequest = new ModalityRequest
            {
                Country = "US",
                UserLcid = 1033,
                Locale = "en-us",
                UserType = "Commercial",
                Source = "PPAC",
                SupportTicketAttributes = new SupportTicketAttribute()
            };

            var result = await controller.GetAvailableModalities(modalityRequest, new HeaderDictionary());

            var objectResult = result.Result as ObjectResult;
            Assert.NotNull(objectResult);
            Assert.Equal(200, objectResult.StatusCode);
        }

        [Fact]
        public async Task GetWidgetDetails_ShouldReturnBadRequest_WhenRequestIsNull()
        {
            var controller = CreateController();

            var result = await controller.GetWidgetDetails(null);

            var objectResult = result.Result as ObjectResult;
            Assert.NotNull(objectResult);
            Assert.Equal(400, objectResult.StatusCode);
            // Optionally check the message:
            // Assert.Equal("Request body for GetWidgetDetails is null", objectResult.Value);
        }

        [Fact]
        public async Task GetWidgetDetails_ShouldReturnOk_WhenRequestIsValid()
        {
            var controller = CreateController();

            var widgetRequest = new WidgetRequest
            {
                Source = "PPAC",
                UserLcid = 1033,
                Locale = "en-us",
                UserType = "Commercial"
            };

            var result = await controller.GetWidgetDetails(widgetRequest);

            var objectResult = result.Result as ObjectResult;
            Assert.NotNull(objectResult);
            Assert.Equal(200, objectResult.StatusCode);
        }
    }
}