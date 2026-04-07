using System.Collections.Generic;

namespace ZeusNX.Metadata
{
    internal class ZeusNXMetadata
    {
        public string ZNXVer { get; } = "1.0.0RC6";
        public string TitleID { get; set; }
        public string Version { get; set; }
        public string ProjectPath { get; set; }
        public string KeysPath { get; set; }
        public string ConfigName { get; set; }
        public string SplashPath { get; set; }

        public bool ExistingOptionsCheck { get; set; }
        public bool RequireAccount { get; set; }
        public bool DebugOutput { get; set; }
        public bool EnableFileAccessChecking { get; set; }
        public bool InterpolatePixels { get; set; }
        public bool Scale { get; set; }
        public int texturePage { get; set; }
        public bool UseSplash { get; set; }
        public bool SameIcons { get; set; }
        public bool EnableScreenShots { get; set; }
        public bool EnableVideoCapture { get; set; }
        public string OfflineManualPath { get; set; }

        //languages
        public bool AmericanEnglish { get; set; }
        public bool CanadianFrench { get; set; }
        public bool LatinAmericanSpanish { get; set; }
        public bool BrazilianPortuguese { get; set; }
        public bool Japanese { get; set; }
        public bool SimplifiedChinese { get; set; }
        public bool TraditionalChinese { get; set; }
        public bool Korean { get; set; }
        public bool BritishEnglish { get; set; }
        public bool French { get; set; }
        public bool German { get; set; }
        public bool EuropeanSpanish { get; set; }
        public bool Italian { get; set; }
        public bool Dutch { get; set; }
        public bool Portuguese { get; set; }
        public bool Russian { get; set; }

        public Dictionary<string, string> TitleNames { get; set; }
        public Dictionary<string, string> TitleAuthors { get; set; }
        public Dictionary<string, string> IconPaths { get; set; }
    }
}
