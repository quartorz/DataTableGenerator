using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace DataTableGenerator
{
	internal class IndexSource : IEquatable<IndexSource>
	{
		public AttributeData Attribute { get; }
		public string[] Names { get; }

		public IndexSource(AttributeData attribute)
		{
			Attribute = attribute;
			Names = attribute.ConstructorArguments
				.SelectMany(static x => x.Values.Select(static v => v.Value as string)).ToArray();
		}

		// Used when the key names do not come from the whole attribute's constructor arguments
		// (e.g. [GenerateDataTableStore(typeof(X), "Id")] where the first argument is the row type).
		public IndexSource(AttributeData attribute, string[] names)
		{
			Attribute = attribute;
			Names = names;
		}

		public bool Equals(IndexSource other)
		{
			if (other is null) return false;
			if (ReferenceEquals(this, other)) return true;
			return Names.AsSpan().SequenceEqual(other.Names);
		}

		public override bool Equals(object obj)
		{
			if (obj is null) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != GetType()) return false;
			return Equals((IndexSource)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hash = 17;
				foreach (var n in Names) hash = hash * 31 + (n?.GetHashCode() ?? 0);
				return hash;
			}
		}

		public string ToTypeString(
			GeneratorSource src,
			Dictionary<string, (ISymbol symbol, string qualifiedTypeName)> members)
		{
			using var _ = StringBuilderHolder.Get(out var type);
			for (var i = 0; i < Names.Length; ++i)
			{
				var n = Names[i];
				if (!members.TryGetValue(n, out var member))
				{
					var m = src.Target.GetMembers(n);
					if (m.Length == 0)
					{
						throw new IndexSourceConversionException(IndexSourceConversionException.Error.InvalidIndexName,
							n);
					}
					switch (m[0])
					{
						case IFieldSymbol field:
							member = members[n] = (field, field.Type.QualifiedName());
							break;
						case IPropertySymbol property:
							member = members[n] = (property, property.Type.QualifiedName());
							break;
						default:
							throw new IndexSourceConversionException(
								IndexSourceConversionException.Error.IndexNameMustBePropertyOrField, n);
					}
				}

				if (Names.Length != 1)
				{
					type.Append(i == 0 ? "(" : ", ").Append($"{member.qualifiedTypeName} {n}");
				}
				else
				{
					type.Append(member.qualifiedTypeName);
				}
			}
			if (Names.Length != 1)
			{
				type.Append(')');
			}
			return type.ToString();
		}
	}

	internal class IndexSourceConversionException : Exception
	{
		internal enum Error
		{
			InvalidIndexName,
			IndexNameMustBePropertyOrField,
		}

		public Error ErrorCode { get; }
		public string Name { get; }

		public IndexSourceConversionException(Error errorCode, string name)
		{
			ErrorCode = errorCode;
			Name = name;
		}
	}

	internal class SortKeySource : IEquatable<SortKeySource>
	{
		public string Name { get; }
		public bool Descending { get; }
		public string PropertySegment => Descending ? Name + "Desc" : Name;

		public SortKeySource(string raw)
		{
			if (raw != null && raw.EndsWith(":desc", StringComparison.OrdinalIgnoreCase))
			{
				Name = raw.Substring(0, raw.Length - 5);
				Descending = true;
			}
			else if (raw != null && raw.EndsWith(":asc", StringComparison.OrdinalIgnoreCase))
			{
				Name = raw.Substring(0, raw.Length - 4);
				Descending = false;
			}
			else
			{
				Name = raw;
				Descending = false;
			}
		}

		public bool Equals(SortKeySource other)
		{
			if (other is null) return false;
			if (ReferenceEquals(this, other)) return true;
			return Name == other.Name && Descending == other.Descending;
		}

		public override bool Equals(object obj)
		{
			if (obj is null) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != GetType()) return false;
			return Equals((SortKeySource)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return ((Name != null ? Name.GetHashCode() : 0) * 397) ^ Descending.GetHashCode();
			}
		}
	}

	internal class SortSource : IEquatable<SortSource>
	{
		public AttributeData Attribute { get; }
		public SortKeySource[] Keys { get; }
		public string CombinedName { get; }

		public SortSource(AttributeData attribute)
		{
			Attribute = attribute;
			var rawKeys = attribute.ConstructorArguments
				.SelectMany(static x => x.Values.Select(static v => v.Value as string)).ToArray();
			Keys = rawKeys.Select(static k => new SortKeySource(k)).ToArray();
			CombinedName = string.Join("And", Keys.Select(static k => k.PropertySegment));
		}

		public void Validate(GeneratorSource src, Dictionary<string, (ISymbol symbol, string qualifiedTypeName)> members)
		{
			foreach (var key in Keys)
			{
				var n = key.Name;
				if (members.ContainsKey(n)) continue;
				var m = src.Target.GetMembers(n);
				if (m.Length == 0)
					throw new IndexSourceConversionException(IndexSourceConversionException.Error.InvalidIndexName, n);
				switch (m[0])
				{
					case IFieldSymbol field:
						members[n] = (field, field.Type.QualifiedName());
						break;
					case IPropertySymbol property:
						members[n] = (property, property.Type.QualifiedName());
						break;
					default:
						throw new IndexSourceConversionException(
							IndexSourceConversionException.Error.IndexNameMustBePropertyOrField, n);
				}
			}
		}

		public bool Equals(SortSource other)
		{
			if (other is null) return false;
			if (ReferenceEquals(this, other)) return true;
			return Keys.AsSpan().SequenceEqual(other.Keys);
		}

		public override bool Equals(object obj)
		{
			if (obj is null) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != GetType()) return false;
			return Equals((SortSource)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hash = 17;
				foreach (var k in Keys) hash = hash * 31 + (k?.GetHashCode() ?? 0);
				return hash;
			}
		}
	}

	internal class GeneratorSource : IEquatable<GeneratorSource>
	{
		public GeneratorAttributeSyntaxContext Context { get; }

		// The type whose members (key columns, indexes, sorts) are read.
		public INamedTypeSymbol Target { get; }

		// Namespace / hint-name seed of the generated store.
		public string Namespace { get; }
		public string Name { get; }

		// The exact class name that is generated. For the in-place [DataTable] form this is
		// "{Target.Name}Store"; for the [GenerateDataTableStore(typeof(X))] form it is the annotated
		// partial class itself.
		public string StoreClassName { get; }

		// Fully-qualified name of the row type (Target).
		public string QualifiedName { get; }
		public IndexSource UniqueKeySource { get; }
		public IndexSource[] IndexSources { get; }
		public SortSource[] SortSources { get; }

		// In-place form: [DataTable] is on the row type itself; store is "{RowType}Store".
		public GeneratorSource(GeneratorAttributeSyntaxContext context, INamedTypeSymbol target,
			IndexSource uniqueKeySource, IndexSource[] indexSources, SortSource[] sortSources)
		{
			Context = context;
			Target = target;
			Namespace = target.ContainingNamespace.IsGlobalNamespace ? null : target.ContainingNamespace.ToString();
			Name = target.Name;
			StoreClassName = target.Name + "Store";
			QualifiedName = target.QualifiedName();
			UniqueKeySource = uniqueKeySource;
			IndexSources = indexSources;
			SortSources = sortSources;
		}

		// Targeted form: [GenerateDataTableStore(typeof(rowType), ...)] is on storeClass; the store body
		// is generated into storeClass, while members are read from rowType.
		public GeneratorSource(GeneratorAttributeSyntaxContext context, INamedTypeSymbol rowType,
			INamedTypeSymbol storeClass, IndexSource uniqueKeySource, IndexSource[] indexSources,
			SortSource[] sortSources)
		{
			Context = context;
			Target = rowType;
			Namespace = storeClass.ContainingNamespace.IsGlobalNamespace
				? null
				: storeClass.ContainingNamespace.ToString();
			Name = storeClass.Name;
			StoreClassName = storeClass.Name;
			QualifiedName = rowType.QualifiedName();
			UniqueKeySource = uniqueKeySource;
			IndexSources = indexSources;
			SortSources = sortSources;
		}

		public bool Equals(GeneratorSource other)
		{
			if (other is null) return false;
			if (ReferenceEquals(this, other)) return true;
			return Namespace == other.Namespace && Name == other.Name &&
				StoreClassName == other.StoreClassName && QualifiedName == other.QualifiedName &&
				UniqueKeySource.Equals(other.UniqueKeySource) &&
				IndexSources.AsSpan().SequenceEqual(other.IndexSources) &&
				SortSources.AsSpan().SequenceEqual(other.SortSources);
		}

		public override bool Equals(object obj)
		{
			if (obj is null) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != GetType()) return false;
			return Equals((GeneratorSource)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = (Namespace != null ? Namespace.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (StoreClassName != null ? StoreClassName.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (QualifiedName != null ? QualifiedName.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (UniqueKeySource?.GetHashCode() ?? 0);
				foreach (var idx in IndexSources) hashCode = (hashCode * 397) ^ (idx?.GetHashCode() ?? 0);
				foreach (var sort in SortSources) hashCode = (hashCode * 397) ^ (sort?.GetHashCode() ?? 0);
				return hashCode;
			}
		}
	}

}