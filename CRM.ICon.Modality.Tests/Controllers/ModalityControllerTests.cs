using CRM.ICon.Modality.Controllers;
using CRM.ICon.Modality.Helpers.Cosmos;
using CRM.ICon.Modality.Helpers.Telemetry;
using CRM.ICon.Modality.Services.Omnichannel;
using CRM.ICon.Modality.Services.VDM;
using Microsoft.AspNetCore.Authorization;
using NSubstitute;

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
    }
}