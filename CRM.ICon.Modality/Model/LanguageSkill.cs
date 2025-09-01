namespace CRM.ICon.Modality.Model
{
    public enum LanguageSkill
    {
            /// <summary>
            /// NONE skill, which MUST be never used in the real scenario
            /// </summary>
            NONE = -1,

            /// <summary>
            /// English language skill
            /// </summary>
            ENG = 0,

            /// <summary>
            /// Tranditional Chinese language skill
            /// </summary>
            ZHO = 1,

            /// <summary>
            /// French language skill
            /// </summary>
            FRA = 2,

            /// <summary>
            /// German language skill
            /// </summary>
            DEU = 3,

            /// <summary>
            /// Italian language skill
            /// </summary>
            ITA = 4,

            /// <summary>
            /// Japanese language skill
            /// </summary>
            JPN = 5,

            /// <summary>
            /// Spanish language skill
            /// </summary>
            SPA = 6,

            /// <summary>
            /// Brazil Portuguese language skill
            /// </summary>
            POR = 7,

            /// <summary>
            /// Portugal Portuguese language skill
            /// </summary>
            PORPT = 8,

            /// <summary>
            /// Polish language skill
            /// </summary>
            POL = 9,

            /// <summary>
            /// Turkish language skill
            /// </summary>
            TUR = 10,

            /// <summary>
            /// Russian language skill
            /// </summary>
            RUS = 11,

            /// <summary>
            /// Czech language skill
            /// </summary>
            CES = 12,

            /// <summary>
            /// Hebrew language skill
            /// </summary>
            HEB = 13,

            /// <summary>
            /// Hungarian language skill
            /// </summary>
            HUN = 14,

            /// <summary>
            /// Arabic language skill
            /// </summary>
            ARA = 15,

            /// <summary>
            /// Korean language skill
            /// </summary>
            KOR = 16,

            /// <summary>
            /// Thai language skill
            /// </summary>
            THA = 17,

            /// <summary>
            /// Indonesian language skill
            /// </summary>
            IND = 18,

            /// <summary>
            /// Chinese TW language skill
            /// </summary>
            ZHOTW = 19,

            /// <summary>
            /// Chinese HK language skill
            /// </summary>
            ZHOHK = 20,

            /// <summary>
            /// Danish language skill
            /// </summary>
            DAN = 21,

            /// <summary>
            /// Dutch language skill
            /// </summary>
            NLD = 22,

            /// <summary>
            /// Finnish language skill
            /// </summary>
            FIN = 23,

            /// <summary>
            /// Greek language skill
            /// </summary>
            ELL = 24,

            /// <summary>
            /// Norwegian language skill
            /// </summary>
            NOR = 25,

            /// <summary>
            /// Romanian language skill
            /// </summary>
            RON = 26,

            /// <summary>
            /// Slovak language skill
            /// </summary>
            SLK = 27,

            /// <summary>
            /// Swedish language skill
            /// </summary>
            SWE = 28,

            /// <summary>
            /// Vietnamese language skill
            /// </summary>
            VIE = 29,

            /// <summary>
            /// Ukrainian language skill
            /// </summary>
            UKR = 30,

            /// <summary>
            /// Croatian language skill
            /// </summary>
            HRV = 31,

            /// <summary>
            /// Estonian language skill
            /// </summary>
            EST = 32,

            /// <summary>
            /// Serbian language skill
            /// </summary>
            SRP = 33,

            /// <summary>
            /// Latvian language skill
            /// </summary>
            LAV = 34,

            /// <summary>
            /// Lithuanian language skill
            /// </summary>
            LIT = 35,

            /// <summary>
            /// Bulgarian language skill
            /// </summary>
            BUL = 36,

            /// <summary>
            /// Slovenian language skill
            /// </summary>
            SLV = 37,

            /// <summary>
            /// Malay (Bahasa Melayu) language skill
            /// </summary>
            MSA = 38,

            /// <summary>
            /// Tagalog (Filipino) language skill
            /// </summary>
            FIL = 39,

            /// <summary>
            /// Macedonian language skills
            /// </summary>
            MKD = 40,

            /// <summary>
            /// US geography, United States
            /// </summary>
            GEOUS = 65536,

            /// <summary>
            /// Non-Native English Affinity Geography, Netherlands, Sweden, Denmark, South Africa, Norway, Finland
            /// </summary>
            GEONNE = 65537,

            /// <summary>
            /// Region Nother American
            /// </summary>
            RGNNA = 131072,

            /// <summary>
            /// Region LATAM
            /// </summary>
            RGNLATAM = 131073,

            /// <summary>
            /// Region Europe
            /// </summary>
            RGNEU = 131074,

            /// <summary>
            /// Region Taiwan
            /// </summary>
            RGNTW = 131075,

            /// <summary>
            /// Direct Agent Offer.
            /// </summary>
            DAO = 196608,

            /// <summary>
            /// GIG skill.
            /// </summary>
            GIG = 196609,

            /// <summary>
            /// Directive Skill
            /// </summary>
            Directive = 262144,

            /// <summary>
            /// Partner Skill
            /// </summary>
            Partner = 262145,

            /// <summary>
            /// Unclassifed Skill
            /// </summary>
            Unclassified = 327680,

            /// <summary>
            /// Alchemy Skill
            /// </summary>
            Alchemy = 327681,

            /// <summary>
            /// AlchemyPrime Skill
            /// </summary>
            AlchemyPrime = 327682,

            /// <summary>
            /// SmartAgent Skill
            /// </summary>
            SmartAgent = 598234,

            /// <summary>
            /// Premier Skill
            /// </summary>
            Premier = 393216,

            /// <summary>
            /// Regular Support Skill
            /// </summary>
            RegularSupport = 393217,

            /// <summary>
            /// Premier Escalation Type
            /// </summary>
            PremierEscalation = 589824,

            /// <summary>
            /// CritSit Type
            /// </summary>
            CritSit = 589825,

            /// <summary>
            /// Consumer skill
            /// </summary>
            Consumer = 589826,

            /// <summary>
            /// Level 2 Assistance Request skill
            /// </summary>
            L2AR = 589827,

            /// <summary>
            /// EMEA ModernRegion Skill
            /// </summary>
            EMEA = 458752,

            /// <summary>
            /// APGC ModernRegion Skill
            /// </summary>
            APGC = 458753,

            /// <summary>
            /// NOAM ModernRegion Skill
            /// </summary>
            NOAM = 458754,

            /// <summary>
            /// LATAM ModernRegion Skill
            /// </summary>
            LATAM = 458755,

            /// <summary>
            /// Beginner Level Skill
            /// </summary>
            Novice = 524288,

            /// <summary>
            /// Contender Level Skill
            /// </summary>
            Contender = 524289,

            /// <summary>
            /// Advance Contender Level Skill
            /// </summary>
            AdvanceContender = 524290,

            /// <summary>
            /// GCC Gov Skill
            /// </summary>
            GCCGov = 524291,

            /// <summary>
            /// Tier0 Skill
            /// </summary>
            Tier0 = 1048576,

            /// <summary>
            /// Tier1 Skill
            /// </summary>
            Tier1 = 1048577,

            /// <summary>
            /// Tier2 Skill
            /// </summary>
            Tier2 = 1048578,

            /// <summary>
            /// Tier3 Skill
            /// </summary>
            Tier3 = 1048579,

            /// <summary>
            /// USNational Skill
            /// </summary>
            USNational = 628096,

            /// <summary>
            /// SingleSeat Skill
            /// </summary>
            LicenseVSB = 700000,

            /// <summary>
            /// CitizenAlliance Skill
            /// </summary>
            CitizenAlliance = 628097
    }
}
