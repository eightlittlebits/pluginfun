using elb_utilities.Configuration;

namespace pluginfun.common
{
    public abstract class EmulatedSystemConfiguration : XmlConfiguration
    {
        public abstract EmulatedSystemConfiguration Copy();
    }
}
