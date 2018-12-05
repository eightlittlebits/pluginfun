using System.Collections.Generic;
using System.Xml.Serialization;
using pluginfun.Configuration;

namespace pluginfun
{
    public class PluginfunConfig : XmlConfiguration<PluginfunConfig>
    {
        protected override string Name => "pluginfun.xml";

        [XmlArray]
        public List<string> RecentFiles { get; set; } = new List<string>();
    }
}
