using System.ComponentModel;
using elb_utilities.Configuration;

namespace pluginfun
{
    public interface ISystemConfiguration
    {
    }

    public class MasterSystemConfiguration : XmlConfiguration<MasterSystemConfiguration>, ISystemConfiguration
    {
        protected override string Name => "mastersystem.xml";

        [Description("BIOS Path")]
        public string BiosPath { get; set; }

        [Description("PAL System")]
        public bool PalSystem { get; set; }
    }
}
