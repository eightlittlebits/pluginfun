using elb_utilities.Configuration;

namespace pluginfun.shared
{
    public abstract class EmulatedSystemConfiguration : XmlConfiguration
    {
        public abstract EmulatedSystemConfiguration Copy();
    }
}
