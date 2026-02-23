using System.Collections.Generic;
using System.Xml.Serialization;

namespace ZeusNX.NMeta
{
    [XmlRoot(ElementName = "Title")]
    public class Title
    {
        [XmlElement(ElementName = "Language")]
        public string Language { get; set; }

        [XmlElement(ElementName = "Name")]
        public string Name { get; set; }

        [XmlElement(ElementName = "Publisher")]
        public string Publisher { get; set; }
    }

    [XmlRoot(ElementName = "Application")]
    public class Application
    {
        [XmlElement(ElementName = "Title")]
        public List<Title> Title { get; set; }

        [XmlElement(ElementName = "Isbn")]
        public string Isbn { get; set; } = string.Empty;

        [XmlElement(ElementName = "StartupUserAccount")]
        public string StartupUserAccount { get; set; }

        [XmlElement(ElementName = "UserAccountSwitchLock")]
        public string UserAccountSwitchLock { get; set; } = "Disable";

        [XmlElement(ElementName = "ParentalControl")]
        public string ParentalControl { get; set; } = "None";

        [XmlElement(ElementName = "SupportedLanguage")]
        public List<string> SupportedLanguage { get; set; }

        [XmlElement(ElementName = "Screenshot")]
        public string Screenshot { get; set; }

        [XmlElement(ElementName = "VideoCapture")]
        public string VideoCapture { get; set; }

        [XmlElement(ElementName = "PresenceGroupId")]
        public string PresenceGroupId { get; set; }

        [XmlElement(ElementName = "DisplayVersion")]
        public string DisplayVersion { get; set; }

        [XmlElement(ElementName = "DataLossConfirmation")]
        public string DataLossConfirmation { get; set; } = "None";

        [XmlElement(ElementName = "PlayLogPolicy")]
        public string PlayLogPolicy { get; set; } = "All";

        [XmlElement(ElementName = "SaveDataOwnerId")]
        public string SaveDataOwnerId { get; set; }

        [XmlElement(ElementName = "UserAccountSaveDataSize")]
        public string UserAccountSaveDataSize { get; set; } = "0x0000000000400000";

        [XmlElement(ElementName = "UserAccountSaveDataJournalSize")]
        public string UserAccountSaveDataJournalSize { get; set; } = "0x0000000000400000";

        [XmlElement(ElementName = "DeviceSaveDataSize")]
        public string DeviceSaveDataSize { get; set; } = "0x0000000000000000";

        [XmlElement(ElementName = "DeviceSaveDataJournalSize")]
        public string DeviceSaveDataJournalSize { get; set; } = "0x0000000000000000";

        [XmlElement(ElementName = "BcatDeliveryCacheStorageSize")]
        public string BcatDeliveryCacheStorageSize { get; set; } = "0x0000000000000000";

        [XmlElement(ElementName = "ApplicationErrorCodeCategory")]
        public string ApplicationErrorCodeCategory { get; set; } = "ZUSNX";

        [XmlElement(ElementName = "AddOnContentBaseId")]
        public string AddOnContentBaseId { get; set; }

        [XmlElement(ElementName = "LogoType")]
        public string LogoType { get; set; } = "LicensedByNintendo";

        [XmlElement(ElementName = "LocalCommunicationId")]
        public string LocalCommunicationId { get; set; }

        [XmlElement(ElementName = "LogoHandling")]
        public string LogoHandling { get; set; } = "Auto";

        [XmlElement(ElementName = "SeedForPseudoDeviceId")]
        public string SeedForPseudoDeviceId { get; set; }

        [XmlElement(ElementName = "BcatPassphrase")]
        public string BcatPassphrase { get; set; } = string.Empty;

        [XmlElement(ElementName = "AddOnContentRegistrationType")]
        public string AddOnContentRegistrationType { get; set; } = "OnDemand";

        [XmlElement(ElementName = "UserAccountSaveDataSizeMax")]
        public string UserAccountSaveDataSizeMax { get; set; } = "0x0000000000000000";

        [XmlElement(ElementName = "UserAccountSaveDataJournalSizeMax")]
        public string UserAccountSaveDataJournalSizeMax { get; set; } = "0x0000000000000000";

        [XmlElement(ElementName = "DeviceSaveDataSizeMax")]
        public string DeviceSaveDataSizeMax { get; set; } = "0x0000000000000000";

        [XmlElement(ElementName = "DeviceSaveDataJournalSizeMax")]
        public string DeviceSaveDataJournalSizeMax { get; set; } = "0x0000000000000000";

        [XmlElement(ElementName = "TemporaryStorageSize")]
        public string TemporaryStorageSize { get; set; } = "0x0000000000000000";

        [XmlElement(ElementName = "CacheStorageSize")]
        public string CacheStorageSize { get; set; } = "0x0000000000000000";

        [XmlElement(ElementName = "CacheStorageJournalSize")]
        public string CacheStorageJournalSize { get; set; } = "0x0000000000000000";

        [XmlElement(ElementName = "CacheStorageDataAndJournalSizeMax")]
        public string CacheStorageDataAndJournalSizeMax { get; set; } = "0x0000000000000000";

        [XmlElement(ElementName = "CacheStorageIndexMax")]
        public string CacheStorageIndexMax { get; set; } = "0x0000000000000000";

        [XmlElement(ElementName = "Hdcp")]
        public string Hdcp { get; set; } = "None";

        [XmlElement(ElementName = "CrashReport")]
        public string CrashReport { get; set; } = "Allow";

        [XmlElement(ElementName = "RuntimeAddOnContentInstall")]
        public string RuntimeAddOnContentInstall { get; set; } = "Deny";
        [XmlElement(ElementName = "PlayLogQueryableApplicationId")]
        public string PlayLogQueryableApplicationId { get; set; } = "0x0000000000000000";

        [XmlElement(ElementName = "PlayLogQueryCapability")]
        public string PlayLogQueryCapability { get; set; } = "None";

        [XmlElement(ElementName = "Repair")]
        public string Repair { get;  } = "None";

        [XmlElement(ElementName = "Attribute")]
        public string Attribute { get; set; } = "None";

        [XmlElement(ElementName = "ProgramIndex")]
        public int ProgramIndex { get; set; } = 0;

        [XmlElement(ElementName = "RequiredNetworkServiceLicenseOnLaunch")]
        public string RequiredNetworkServiceLicenseOnLaunch { get; set; } = "None";
    }
}
