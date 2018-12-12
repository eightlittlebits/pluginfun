using System.ComponentModel;
using pluginfun.shared;

namespace pluginfun
{
    public enum Region
    {
        NTSC,
        PAL
    }

    public sealed class SystemConfiguration : EmulatedSystemConfiguration
    { 
        protected override sealed string FileName => "systemconfig.xml";

        [Description("Use Boot ROM")]
        public bool BootRomEnabled { get; set; }

        [Description("Boot ROM Path"), FilePath]
        public string BootRomPath { get; set; }

        [Description("Save State Path"), FolderPath]
        public string SaveStatePath { get; set; }

        public Region Region { get; set; }

        public override EmulatedSystemConfiguration Copy()
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
