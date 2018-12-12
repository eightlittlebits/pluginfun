using System;

namespace pluginfun.shared
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
