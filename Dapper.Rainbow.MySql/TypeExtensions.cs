using System;
using System.Reflection;

namespace Dapper
{
    internal static class TypeExtensions
    {
        public static bool IsGenericType(this Type type) =>
#if NETSTANDARD1_3 || NETCOREAPP1_0
            type.GetTypeInfo().IsGenericType;
#else
            type.IsGenericType;
#endif

        public static bool IsValueType(this Type type) =>
#if NETSTANDARD1_3 || NETCOREAPP1_0
            type.GetTypeInfo().IsValueType;
#else
            type.IsValueType;
#endif
    }
}