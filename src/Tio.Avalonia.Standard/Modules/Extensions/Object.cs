namespace Tio.Avalonia.Standard.Modules.Extensions;

public static class Object
{
    public static bool IsNull(this object? obj)
    {
        return obj == null;
    }
}