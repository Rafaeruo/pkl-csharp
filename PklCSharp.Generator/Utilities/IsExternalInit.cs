// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices;

// Hack to make init-only values (eg records) work in older .net versions
internal static class IsExternalInit
{
}