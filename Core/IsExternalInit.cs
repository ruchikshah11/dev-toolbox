namespace System.Runtime.CompilerServices
{
    // net472 doesn't ship IsExternalInit - modern C# needs it as a marker type to allow
    // `init` accessors and `record`/`record struct` types to compile against this target.
    internal static class IsExternalInit
    {
    }
}
