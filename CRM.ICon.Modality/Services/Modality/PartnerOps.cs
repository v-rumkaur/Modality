namespace CRM.ICon.Modality.Services.Modality
{
    public static class PartnerOps
    {
        public static PartnerOp GetCompassContentAsyncOp = new PartnerOp(
                "MSEG.CE.Library.SF.Common",
                "ICompassService.GetCompassContentAsync",
                "Gets Compass content for a given product path, locale, and preview parameter");

        public static PartnerOp GetCompassFallbackLocalesOp = new PartnerOp(
            "MSEG.CE.Library.SF.Common",
            "ICompassService.GetFallbackLocales",
            "Gets Compass fallback locales");

        public static PartnerOp ExecuteDocumentDbQueryOp = new PartnerOp(
            "DocumentDb",
            "ExecuteQueryAsync",
            "Gets the data of circuit breaker");

        public static PartnerOp AwasaQueueScheduleServiceGetScheduleOp = new PartnerOp(
            "Awasa",
            "AwasaQueueScheduleService.GetSchedule",
            "Gets the queue schedule for a given modality, subject, language, country, mode, disability, api-version, and tier");

        public static PartnerOp AwasaQueueLengthServiceGetLength = new PartnerOp(
            "Awasa",
            "AwasaQueueLengthService.GetLength",
            "Gets the length of the queue for a given subject, country, language, mode, and disability");

        public static PartnerOp AwasaChannelServiceGetChannelOp = new PartnerOp(
            "Awasa",
            "AwasaChannelService.GetChannel",
            "Gets the channel for a given subject, language, and country");

        public static PartnerOp CantileverQueueScheduleServiceGetScheduleOp = new PartnerOp(
            "Cantilever",
            "CantileverQueueScheduleService.GetSchedule",
            "Gets the schedule for a given subject, language, country, mode, and tier.");

        public static PartnerOp CantileverQueueLengthServiceGetLength = new PartnerOp(
            "Cantilever",
            "CantileverQueueLengthService.GetLength",
            "Gets the length of the queue for a given subject, language, country, mode, and tier");

        public static PartnerOp CantileverChannelServiceGetChannelOp = new PartnerOp(
            "Cantilever",
            "CantileverChannelService.GetChannel",
            "Gets the channel for a given subject, language, and country");

        public static PartnerOp PlatformServiceGetPlatformOp = new PartnerOp(
            "Cantilever",
            "PlatformService.GetPlatform",
            "Gets the platform for a given subject, language, and country");
    }
}