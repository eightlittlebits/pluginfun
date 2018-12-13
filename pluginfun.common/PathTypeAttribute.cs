using System;

namespace pluginfun.common
{
    [AttributeUsage(AttributeTargets.Property)]
    public abstract class PathTypeAttribute : Attribute
    {

    }
    
    public class FilePathAttribute : PathTypeAttribute
    {

    }

    public class FolderPathAttribute : PathTypeAttribute
    {

    }
}
