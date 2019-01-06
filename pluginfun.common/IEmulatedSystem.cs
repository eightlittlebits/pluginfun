namespace pluginfun.common
{
    public interface IEmulatedSystem
    {
        string Name { get; }

        IConfiguration Configuration { get; set; }
    }
}
