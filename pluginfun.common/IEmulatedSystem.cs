namespace pluginfun.common
{
    public interface IEmulatedSystem
    {
        string Name { get; }

        EmulatedSystemConfiguration Configuration { get; set; }
    }
}
