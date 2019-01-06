using elb_utilities.Configuration;
using pluginfun.common;

namespace pluginfun
{
    internal class EmulatedSystem : IEmulatedSystem
    {
        public string Name => "Emulated System";

        private SystemConfiguration _configuration;
        public IConfiguration Configuration
        {
            get => _configuration;
            set => _configuration = (SystemConfiguration)value;
        }

        public EmulatedSystem()
        {
            _configuration = SystemConfiguration.Load();
        }
    }
}
