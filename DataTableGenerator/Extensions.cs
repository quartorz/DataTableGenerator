using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace DataTableGenerator
{
	internal static class Extensions
	{
		public static IEnumerable<AttributeData> FindAttributes<T>(this T self, string namespaceName, string name) where T : ISymbol
		{
			foreach (var attribute in self.GetAttributes())
			{
				var cls = attribute.AttributeClass;
				if (cls!.ContainingNamespace.ToString() == namespaceName && cls.Name == name)
				{
					yield return attribute;
				}
			}
		}
		
		public static AttributeData FindAttribute<T>(this T self, string namespaceName, string name) where T : ISymbol
		{
			foreach (var attribute in self.GetAttributes())
			{
				var cls = attribute.AttributeClass;
				if (cls!.ContainingNamespace.ToString() == namespaceName && cls.Name == name)
				{
					return attribute;
				}
			}

			return null;
		}

		public static bool HasAttribute<T>(this T self, string namespaceName, string name) where T : ISymbol
		{
			return self.FindAttribute(namespaceName, name) != null;
		}

		public static string QualifiedName(this ITypeSymbol self)
		{
			switch (self)
			{
				case IArrayTypeSymbol array:
					return array.QualifiedName();
				case INamedTypeSymbol named:
					return named.QualifiedName();
				case ITypeParameterSymbol typeParameter:
					return typeParameter.QualifiedName();
			}

			return $"not implemented: {self.GetType()}";
		}

		public static string QualifiedName(this IArrayTypeSymbol self)
		{
			return $"{self.ElementType.QualifiedName()}[{string.Join("", Enumerable.Repeat(",", self.Rank - 1))}]";
		}

		public static string QualifiedName(this INamedTypeSymbol self)
		{
			using var _ = StringBuilderHolder.Get(out var sb);
			if (self.ContainingNamespace.IsGlobalNamespace)
			{
				sb.Append("global::");
			}
			else
			{
				sb.Append(self.ContainingNamespace).Append('.');
			}
			
			var containingType = self.ContainingType;
			while (containingType != null)
			{
				sb.Append(containingType.Name).Append(containingType.TypeArguments.GenericArguments()).Append('.');
				containingType = containingType.ContainingType;
			}
			
			sb.Append(self.Name).Append(self.TypeArguments.GenericArguments());
			return sb.ToString();
		}

		public static string QualifiedName(this ITypeParameterSymbol self)
		{
			return self.Name;
		}

		public static string GenericArguments(this ImmutableArray<ITypeSymbol> self)
		{
			if (self.Length == 0)
			{
				return "";
			}
			
			using var _ = StringBuilderHolder.Get(out var sb);
			sb.Append('<');
			for (var i = 0; i < self.Length; ++i)
			{
				if (i > 0)
				{
					sb.Append(',');
				}

				sb.Append(self[i].QualifiedName());
			}
			sb.Append('>');
			return sb.ToString();
		}

		public static string ReturnTypeName(this IMethodSymbol self)
		{
			if (self.ReturnsVoid)
			{
				return "void";
			}

			return self.ReturnType.QualifiedName();
		}
	}
}