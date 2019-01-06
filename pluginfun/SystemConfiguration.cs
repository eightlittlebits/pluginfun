using System.ComponentModel;
using elb_utilities.Configuration;
using pluginfun.common;

namespace pluginfun
{
    public enum Region
    {
        NTSC,
        PAL
    }

    public sealed class SystemConfiguration : XmlConfiguration<SystemConfiguration>, IConfiguration
    { 
        protected override sealed string FileName => "systemconfig.xml";

        [Description("Use Boot ROM")]
        public bool BootRomEnabled { get; set; }

        [Description("Boot ROM Path"), FilePath]
        public string BootRomPath { get; set; }

        [Description("Save State Path"), FolderPath]
        public string SaveStatePath { get; set; }

        public Region Region { get; set; }

        public IConfiguration Copy()
        {
            return new SystemConfiguration()
            {
                BootRomEnabled = this.BootRomEnabled,
                BootRomPath = this.BootRomPath,
                SaveStatePath = this.SaveStatePath,
                Region = this.Region
            };
        }
    }
}
