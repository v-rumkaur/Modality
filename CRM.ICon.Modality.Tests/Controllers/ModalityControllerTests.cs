using CRM.ICon.Modality.Controllers;
using CRM.ICon.Modality.Helpers.Cosmos;
using CRM.ICon.Modality.Helpers.Telemetry;
using CRM.ICon.Modality.Model;
using CRM.ICon.Modality.Services.Omnichannel;
using CRM.ICon.Modality.Services.VDM;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Serialization.HybridRow;
using Newtonsoft.Json;
using NSubstitute;
using OpenTelemetry.Trace;

namespace CRM.ICon.Modality.Tests.Controllers
{
    public class ModalityControllerTests
    {
        [Theory]
        [InlineData("GetAvailableModalities")]
        [InlineData("GetWidgetDetails")]
        [InlineData("CreateThemeSubjectMapping")]
        public async Task ModalityController_AllMethodsShouldImplementAuthorizeAttribute(string methodName)
        {
            var vdmService = Substitute.For<IVDMService>();
            var omnichannelService = Substitute.For<IOmnichannelService>();
            var telemetryService = Substitute.For<ITelemetryService>();
            var omniChannelEUService = Substitute.For<IOmnichannelEUService>();
            var cosmosDbClient = Substitute.For<ICosmosDbClient>();
            var controller = new ModalityController(vdmService, omnichannelService, telemetryService, omniChannelEUService, cosmosDbClient);

            var type = controller.GetType();
            var methodInfo = type.GetMethod(methodName);
            var attributes = methodInfo.GetCustomAttributes(typeof(AuthorizeAttribute), true);

            Assert.True(attributes.Any()); //Ensure [Authorize] Attribute exist
        }

        [Fact]
        public async Task ModalityController_GetAvailableModalitiesBadRequest()
        {
            var vdmService = Substitute.For<IVDMService>();
            var omnichannelService = Substitute.For<IOmnichannelService>();
            var telemetryService = Substitute.For<ITelemetryService>();
            var omniChannelEUService = Substitute.For<IOmnichannelEUService>();
            var cosmosDbClient = Substitute.For<ICosmosDbClient>();
            var controller = new ModalityController(vdmService, omnichannelService, telemetryService, omniChannelEUService, cosmosDbClient);

            var result = await controller.GetAvailableModalities(null, new HeaderDictionary());
            
            var statusCode = (StatusCodeResult)result.Result;
                      
            Assert.True(statusCode.StatusCode.Equals(400));
        }

        [Fact]
        public async Task ModalityController_GetAvailableModalitiesSuccess()
        {
            var vdmService = Substitute.For<IVDMService>();
            var omnichannelService = Substitute.For<IOmnichannelService>();
            var telemetryService = Substitute.For<ITelemetryService>();
            var omniChannelEUService = Substitute.For<IOmnichannelEUService>();
            var cosmosDbClient = Substitute.For<ICosmosDbClient>();
            var controller = new ModalityController(vdmService, omnichannelService, telemetryService, omniChannelEUService, cosmosDbClient);

            ModalityRequest modalityRequest = new ModalityRequest();
            modalityRequest.Country = "US";
            modalityRequest.UserLcid = 1033;
            modalityRequest.Locale = "en-us";
            modalityRequest.UserType = "Commercial";
            modalityRequest.Source = "PPAC";
            modalityRequest.SupportTicketAttributes = new SupportTicketAttribute();

            var result = await controller.GetAvailableModalities(modalityRequest, new HeaderDictionary());

            var statusCode = (ObjectResult)result.Result;

            Assert.True(statusCode.StatusCode.Equals(200));
        }

        [Fact]
        public async Task ModalityController_GetWidgetDetailsBadRequest()
        {
            var vdmService = Substitute.For<IVDMService>();
            var omnichannelService = Substitute.For<IOmnichannelService>();
            var telemetryService = Substitute.For<ITelemetryService>();
            var omniChannelEUService = Substitute.For<IOmnichannelEUService>();
            var cosmosDbClient = Substitute.For<ICosmosDbClient>();
            var controller = new ModalityController(vdmService, omnichannelService, telemetryService, omniChannelEUService, cosmosDbClient);

            var result = await controller.GetWidgetDetails(null);

            var statusCode = (StatusCodeResult)result.Result;

            Assert.True(statusCode.StatusCode.Equals(400));
        }

        [Fact]
        public async Task ModalityController_GetWidgetDetailsSuccess()
        {
            var vdmService = Substitute.For<IVDMService>();
            var omnichannelService = Substitute.For<IOmnichannelService>();
            var telemetryService = Substitute.For<ITelemetryService>();
            var omniChannelEUService = Substitute.For<IOmnichannelEUService>();
            var cosmosDbClient = Substitute.For<ICosmosDbClient>();
            var controller = new ModalityController(vdmService, omnichannelService, telemetryService, omniChannelEUService, cosmosDbClient);

            WidgetRequest widgetDetails = new WidgetRequest();
            widgetDetails.Source = "PPAC";
            widgetDetails.UserLcid = 1033;
            widgetDetails.Locale = "en-us";
            widgetDetails.UserType = "Commercial";

            var result = await controller.GetWidgetDetails(widgetDetails);

            var statusCode = (ObjectResult)result.Result;

            Assert.True(statusCode.StatusCode.Equals(200));
        }
    }
}