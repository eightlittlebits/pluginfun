using System;
using System.ComponentModel;
using elb_utilities.Configuration;

namespace pluginfun
{
    public interface ISystemConfiguration
    {
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class PathTypeAttribute : Attribute
    {

    }

    public class FilePathAttribute : PathTypeAttribute
    {

    }

    public class FolderPathAttribute : PathTypeAttribute
    {

    }

    public enum Region
    {
        NTSC,
        PAL
    }

    public sealed class MasterSystemConfiguration : XmlConfiguration<MasterSystemConfiguration>, ISystemConfiguration
    {
        protected override sealed string Name => "mastersystem.xml";

        [Description("Use Boot ROM")]
        public bool BootRomEnabled { get; set; }

        [Description("Boot ROM Path"), FilePath]
        public string BootRomPath { get; set; }

        [Description("Save State Path"), FolderPath]
        public string SaveStatePath { get; set; }

        public Region Region { get; set; }
    }
}
