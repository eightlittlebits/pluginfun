using System.Collections.Generic;
using System.Xml.Serialization;
using elb_utilities.Configuration;

namespace pluginfun
{
    public class PluginfunConfig : XmlConfiguration<PluginfunConfig>
    {
        protected override string FileName => "pluginfun.xml";

        public bool LimitFps { get; set; }
        public bool PauseOnLostFocus { get; set; }
        public bool ForceSquarePixels { get; set; }

        [XmlArray]
        public List<string> RecentFiles { get; set; } = new List<string>();
    }
}
