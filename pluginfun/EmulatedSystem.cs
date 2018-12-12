using elb_utilities.Configuration;
using pluginfun.shared;

namespace pluginfun
{
    internal class EmulatedSystem : IEmulatedSystem
    {
        public string Name => "Emulated System";

        private SystemConfiguration _configuration;
        public EmulatedSystemConfiguration Configuration
        {
            get => _configuration;
            set => _configuration = (SystemConfiguration)value;
        }

        public EmulatedSystem()
        {
            _configuration = XmlConfiguration.Load<SystemConfiguration>();
        }
    }
}
