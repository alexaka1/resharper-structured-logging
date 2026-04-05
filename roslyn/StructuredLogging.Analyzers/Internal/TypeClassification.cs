using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace StructuredLogging.Analyzers.Internal;

internal static class TypeClassification
{
    public static bool NeedsDestructuring(ITypeSymbol? type)
    {
        if (type is null)
        {
            return false;
        }

        if (type is INamedTypeSymbol named &&
            string.Equals(named.ConstructedFrom.ToDisplayString(), "System.Nullable<T>", StringComparison.Ordinal) &&
            named.TypeArguments.Length == 1)
        {
            return NeedsDestructuring(named.TypeArguments[0]);
        }

        if (type.SpecialType is >= SpecialType.System_SByte and <= SpecialType.System_Decimal)
        {
            return false;
        }

        if (type.SpecialType == SpecialType.System_String)
        {
            return false;
        }

        if (string.Equals(type.ToDisplayString(), "System.Guid", StringComparison.Ordinal))
        {
            return false;
        }

        if (type.TypeKind == TypeKind.Enum)
        {
            return false;
        }

        if (type is INamedTypeSymbol dictionary &&
            dictionary.TypeArguments.Length == 2 &&
            string.Equals(dictionary.ConstructedFrom.ToDisplayString(), "System.Collections.Generic.Dictionary<TKey, TValue>", StringComparison.Ordinal))
        {
            return NeedsDestructuring(dictionary.TypeArguments[0]);
        }

        if (type is IArrayTypeSymbol array)
        {
            return NeedsDestructuring(array.ElementType);
        }

        if (type is INamedTypeSymbol namedEnumerable)
        {
            var enumerableType = namedEnumerable.AllInterfaces.FirstOrDefault(i =>
                i.TypeArguments.Length == 1 &&
                string.Equals(i.ConstructedFrom.ToDisplayString(), "System.Collections.Generic.IEnumerable<T>", StringComparison.Ordinal));
            if (enumerableType is not null)
            {
                return NeedsDestructuring(enumerableType.TypeArguments[0]);
            }
        }

        if (type is not INamedTypeSymbol classType)
        {
            return false;
        }

        var initial = true;
        for (var current = classType; current is not null; current = current.BaseType)
        {
            if (current.SpecialType == SpecialType.System_Object)
            {
                return !initial;
            }

            var toStringMethod = current.GetMembers("ToString")
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m => m.Parameters.Length == 0 && m.IsOverride);

            if (toStringMethod is not null)
            {
                return false;
            }

            initial = false;
        }

        return true;
    }

    public static bool IsException(ITypeSymbol? type, Compilation compilation)
    {
        if (type is null)
        {
            return false;
        }

        var exceptionType = compilation.GetTypeByMetadataName("System.Exception");
        return exceptionType is not null && InheritsFrom(type, exceptionType);
    }

    private static bool InheritsFrom(ITypeSymbol type, ITypeSymbol baseType)
    {
        for (var current = type; current is not null; current = (current as INamedTypeSymbol)?.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
            {
                return true;
            }
        }

        return false;
    }
}
