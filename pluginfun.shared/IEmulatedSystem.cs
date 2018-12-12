namespace pluginfun.shared
{
    public interface IEmulatedSystem
    {
        string Name { get; }

        EmulatedSystemConfiguration Configuration { get; set; }
    }
}
