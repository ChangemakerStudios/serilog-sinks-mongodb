

// ReSharper disable once CheckNamespace
namespace System;
#if !NETSTANDARD2_1
internal static class CollectionHelpers
{
    internal static bool Contains(
        this string? str,
        string value,
        StringComparison comparison)
    {
        return str?.IndexOf(value, comparison) >= 0;
    }
}
#endif