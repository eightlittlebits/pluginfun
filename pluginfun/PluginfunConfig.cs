using System.Collections.Generic;
using System.Xml.Serialization;
using elb_utilities.Configuration;

namespace pluginfun
{
    public class PluginfunConfig : XmlConfiguration
    {
        protected override string Name => "pluginfun.xml";

        public bool LimitFps { get; set; }
        public bool PauseOnLostFocus { get; set; }

        [XmlArray]
        public List<string> RecentFiles { get; set; } = new List<string>();
    }
}
