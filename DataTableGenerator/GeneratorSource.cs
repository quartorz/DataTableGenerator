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
			return (Names != null ? Names.GetHashCode() : 0);
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
			return Keys != null ? Keys.GetHashCode() : 0;
		}
	}

	internal class GeneratorSource : IEquatable<GeneratorSource>
	{
		public GeneratorAttributeSyntaxContext Context { get; }
		public INamedTypeSymbol Target { get; }
		public string Namespace { get; }
		public string Name { get; }
		public string QualifiedName { get; }
		public IndexSource UniqueKeySource { get; }
		public IndexSource[] IndexSources { get; }
		public SortSource[] SortSources { get; }

		public GeneratorSource(GeneratorAttributeSyntaxContext context, INamedTypeSymbol target,
			IndexSource uniqueKeySource, IndexSource[] indexSources, SortSource[] sortSources)
		{
			Context = context;
			Target = target;
			Namespace = target.ContainingNamespace.IsGlobalNamespace ? null : target.ContainingNamespace.ToString();
			Name = target.Name;
			QualifiedName = target.QualifiedName();
			UniqueKeySource = uniqueKeySource;
			IndexSources = indexSources;
			SortSources = sortSources;
		}

		public bool Equals(GeneratorSource other)
		{
			if (other is null) return false;
			if (ReferenceEquals(this, other)) return true;
			return Namespace == other.Namespace && Name == other.Name &&
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
				hashCode = (hashCode * 397) ^ (IndexSources != null ? IndexSources.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (SortSources != null ? SortSources.GetHashCode() : 0);
				return hashCode;
			}
		}
	}

	partial class A
	{
	}
}