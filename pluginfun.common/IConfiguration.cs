namespace pluginfun.common
{
    public interface IConfiguration
    {
        IConfiguration Copy();

        void Save();
    }
}
