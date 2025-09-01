namespace CRM.ICon.Modality.Model
{
        public class BusinessHourConstants
        {
            /// <summary>
            /// CET Timezone Name, UTC+1.
            /// </summary>
            public const string CETTimeZoneName = "Central European Standard Time";

            /// <summary>
            /// MSK Timezone Name, UTC+3.
            /// </summary>
            public const string MSKTimeZoneName = "Russian Standard Time";

            /// <summary>
            /// IST Timezone Name, UTC+2.
            /// </summary>
            public const string ISTTimeZoneName = "Israel Standard Time";

            /// <summary>
            /// TRT Timezone Name, UTC+3.
            /// </summary>
            public const string TRTTimeZoneName = "Turkey Standard Time";

            /// <summary>
            /// EET Timezone Name, UTC+2.
            /// </summary>
            public const string EETTimeZoneName = "E. Europe Standard Time";

            /// <summary>
            /// PST Timezone Name, UTC-8.
            /// </summary>
            public const string PSTTimeZoneName = "Pacific Standard Time";

            /// <summary>
            /// CST Timezone Name, UTC+8.
            /// </summary>
            public const string CSTTimeZoneName = "China Standard Time";

            /// <summary>
            /// TST Timezone Name, UTC+8.
            /// </summary>
            public const string TSTTimeZoneName = "Taipei Standard Time";

            /// <summary>
            /// SE Asia Timezone Name, UTC+7.
            /// </summary>
            public const string SEASTTimeZoneName = "SE Asia Standard Time";

            /// <summary>
            /// KST Timezone Name, UTC+9.
            /// </summary>
            public const string KSTTimeZoneName = "Korea Standard Time";

            /// <summary>
            /// US CST Timezone Name, UTC-6.
            /// </summary>
            public const string USCSTTimeZoneName = "Central Standard Time";

            /// <summary>
            /// E. SAST Timezone Name, UTC-6.
            /// </summary>
            public const string ESASTTimeZoneName = "E. South America Standard Time";

            /// <summary>
            /// Tokyo standard timezone, UTC+9.
            /// </summary>
            public const string TokyoTimeZoneName = "Tokyo Standard Time";

            /// <summary>
            /// SA Western standard timezone, UTC-4.
            /// </summary>
            public const string SAWSTTimeZoneName = "SA Western Standard Time";

            /// <summary>
            /// Central Asia standard timezone, UTC+6.
            /// </summary>
            public const string BSTTimeZoneName = "Bangladesh Standard Time";

            /// <summary>
            /// Argentina standard timezone, UTC-3.
            /// </summary>
            public const string ARSTTimeZoneName = "Argentina Standard Time";

            /// <summary>
            /// Uruguay standard timezone, UTC-3.
            /// </summary>
            public const string UYTTimeZoneName = "Montevideo Standard Time";

            /// <summary>
            /// Brazil standard timezone, UTC-3.
            /// </summary>
            public const string BRTTimeZoneName = "E. South America Standard Time";

            /// <summary>
            /// Pacific SA Standard TimeZone, UTC-4.
            /// </summary>
            public const string PSASTTimeZoneName = "Pacific SA Standard Time";

            /// <summary>
            /// Venezuela Standard timezone, UTC-4.
            /// </summary>
            public const string VETTimeZoneName = "Venezuela Standard Time";

            /// <summary>
            /// SA Pacific standard timezone, UTC-5.
            /// </summary>
            public const string SAPSTTimeZoneName = "SA Pacific Standard Time";

            /// <summary>
            /// SA Pacific standard timezone, UTC.
            /// </summary>
            public const string UTC = "UTC";

            /// <summary>
            /// Eastern standard timeone.
            /// </summary>
            public const string ESTTimeZoneName = "Eastern Standard Time";

            /// <summary>
            /// The suffix of the language for Portugal Portuguese
            /// </summary>
            public const string PortgualPortugueseSuffix = "PT";

            /// <summary>
            /// The suffix of the language for TW Chinese
            /// </summary>
            public const string TaiwanChineseSuffix = "TW";

            /// <summary>
            /// The suffix of the language for HK Chinese
            /// </summary>
            public const string HongkongChineseSuffix = "HK";

            /// <summary>
            /// The search cache prefix used by office classes.
            /// </summary>
            public const string SearchCachePrefix = "officecasesearch_";

            /// <summary>
            /// The workspaceId that is stored in the IssueContext used by office classes.
            /// </summary>
            public const string DtmWorkspaceId = "DtmWorkspaceId";

            /// <summary>
            /// The Subject Id of the case.
            /// </summary>
            public const string SubjectId = "SubjectId";

            /// <summary>
            /// The Subject Id of the case.
            /// </summary>
            public const string OfficeSearchFilteringFeatureFlight = "OfficeSearchFilteringFeatureFlight";

            /// <summary>
            /// BusinessHourData.
            /// </summary>
            public static readonly Dictionary<string, List<BusinessHour>> BusinessHourData = new Dictionary<string, List<BusinessHour>>(StringComparer.InvariantCultureIgnoreCase)
        {
            // Business Hours for different language support
            { LanguageSkill.ITA.ToString(), new List<BusinessHour>() { new BusinessHour() { StartHour = 8, EndHour = 21, StartMin = 0, EndMin = 0, StartDayOfWeek = 1, EndDayOfWeek = 5, TimeZoneName = CETTimeZoneName } } },
            { LanguageSkill.DEU.ToString(), new List<BusinessHour>() { new BusinessHour() { StartHour = 8, EndHour = 21, StartMin = 0, EndMin = 0, StartDayOfWeek = 1, EndDayOfWeek = 5, TimeZoneName = CETTimeZoneName } } },
            { LanguageSkill.FRA.ToString(), new List<BusinessHour>() { new BusinessHour() { StartHour = 8, EndHour = 24, StartMin = 0, EndMin = 0, StartDayOfWeek = 1, EndDayOfWeek = 5, TimeZoneName = CETTimeZoneName } } },
            { LanguageSkill.CES.ToString(), new List<BusinessHour>() { new BusinessHour() { StartHour = 8, EndHour = 21, StartMin = 0, EndMin = 0, StartDayOfWeek = 1, EndDayOfWeek = 5, TimeZoneName = CETTimeZoneName } } },
            { LanguageSkill.RUS.ToString(), new List<BusinessHour>() { new BusinessHour() { StartHour = 7, EndHour = 20, StartMin = 0, EndMin = 0, StartDayOfWeek = 1, EndDayOfWeek = 5, TimeZoneName = MSKTimeZoneName } } },
            { LanguageSkill.POL.ToString(), new List<BusinessHour>() { new BusinessHour() { StartHour = 8, EndHour = 21, StartMin = 0, EndMin = 0, StartDayOfWeek = 1, EndDayOfWeek = 5, TimeZoneName = CETTimeZoneName } } },
            { LanguageSkill.TUR.ToString(), new List<BusinessHour>() { new BusinessHour() { StartHour = 8, EndHour = 21, StartMin = 0, EndMin = 0, StartDayOfWeek = 1, EndDayOfWeek = 5, TimeZoneName = TRTTimeZoneName } } },
            { LanguageSkill.HUN.ToString(), new List<BusinessHour>() { new BusinessHour() { StartHour = 8, EndHour = 21, StartMin = 0, EndMin = 0, StartDayOfWeek = 1, EndDayOfWeek = 5, TimeZoneName = CETTimeZoneName } } },
            { LanguageSkill.HEB.ToString(), new List<BusinessHour>() { new BusinessHour() { StartHour = 9, EndHour = 18, StartMin = 0, EndMin = 0, StartDayOfWeek = 1, EndDayOfWeek = 5, TimeZoneName = ISTTimeZoneName } } },
            { LanguageSkill.ARA.ToString(), new List<BusinessHour>() { new BusinessHour() { StartHour = 8, EndHour = 18, StartMin = 0, EndMin = 0, StartDayOfWeek = 1, EndDayOfWeek = 5, TimeZoneName = EETTimeZoneName } } },
            { LanguageSkill.KOR.ToString(), new List<BusinessHour>() { new BusinessHour() { StartHour = 9, EndHour = 18, StartMin = 0, EndMin = 0, StartDayOfWeek = 1, EndDayOfWeek = 5, TimeZoneName = KSTTimeZoneName } } },
            { LanguageSkill.THA.ToString(), new List<BusinessHour>() { new BusinessHour() { StartHour = 9, EndHour = 17, StartMin = 0, EndMin = 0, StartDayOfWeek = 1, EndDayOfWeek = 5, TimeZoneName = SEASTTimeZoneName } } },
            { LanguageSkill.IND.ToString(), new List<BusinessHour>() { new BusinessHour() { StartHour = 8, EndHour = 17, StartMin = 0, EndMin = 0, StartDayOfWeek = 1, EndDayOfWeek = 5, TimeZoneName = CSTTimeZoneName } } },
            { LanguageSkill.ZHO.ToString(), new List<BusinessHour>() { new BusinessHour() { StartHour = 9, EndHour = 18, StartMin = 0, EndMin = 0, StartDayOfWeek = 1, EndDayOfWeek = 5, TimeZoneName = CSTTimeZoneName } } },
            { LanguageSkill.ZHOTW.ToString(), new List<BusinessHour>() { new BusinessHour() { StartHour = 9, EndHour = 18, StartMin = 0, EndMin = 0, StartDayOfWeek = 1, EndDayOfWeek = 5, TimeZoneName = TSTTimeZoneName } } },
            { LanguageSkill.ZHOHK.ToString(), new List<BusinessHour>() { new BusinessHour() { StartHour = 9, EndHour = 18, StartMin = 0, EndMin = 0, StartDayOfWeek = 1, EndDayOfWeek = 5, TimeZoneName = CSTTimeZoneName } } },

        };
    }
}

