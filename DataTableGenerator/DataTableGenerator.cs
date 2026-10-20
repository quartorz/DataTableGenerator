using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("DataTableGeneratorTest")]

namespace DataTableGenerator
{
	[Generator(LanguageNames.CSharp)]
	public class DataTableGenerator : IIncrementalGenerator
	{
		/// <summary>
		/// First lines of every generated store: enables nullable annotations so the <c>?</c> the generator
		/// emits is valid whatever the consuming assembly's nullable setting is.
		/// </summary>
		internal const string GeneratedFileHeader = "#nullable enable\n\n";

		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
			context.RegisterPostInitializationOutput(ctx =>
			{
				ctx.AddSource("DataTableAttribute.cs", /* lang=C#-test, lang=C# */ @"
using System;

namespace DataTableGenerator
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	internal sealed class DataTableAttribute : Attribute
	{
		public string [] UniqueKeys;
		public DataTableAttribute(params string[] uniqueKeys)
		{
			UniqueKeys = uniqueKeys;
		}
	}
}
");

				ctx.AddSource("DataTableIndexAttribute.cs", /* lang=C#-test, lang=C# */ @"
using System;

namespace DataTableGenerator
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	internal sealed class DataTableIndexAttribute : Attribute
	{
		public string[] Names;
		public DataTableIndexAttribute(params string[] names)
		{
			Names = names;
		}
	}
}
");

				ctx.AddSource("DataTableSortAttribute.cs", /* lang=C#-test, lang=C# */ @"
using System;

namespace DataTableGenerator
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	internal sealed class DataTableSortAttribute : Attribute
	{
		public string[] Keys;
		public DataTableSortAttribute(params string[] keys)
		{
			Keys = keys;
		}
	}
}
");

				ctx.AddSource("DataTableSortComparerAttribute.cs", /* lang=C#-test, lang=C# */ @"
using System;

namespace DataTableGenerator
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	internal sealed class DataTableSortComparerAttribute : Attribute
	{
		public string Key;
		public Type ComparerType;
		public DataTableSortComparerAttribute(string key, Type comparerType)
		{
			Key = key;
			ComparerType = comparerType;
		}
	}
}
");

				ctx.AddSource("GenerateDataTableStoreAttribute.cs", /* lang=C#-test, lang=C# */ @"
using System;

namespace DataTableGenerator
{
	// Targeted form: put this on a partial class to generate the store body into it, reading the row
	// type given by typeof(...). Use when the row type itself cannot carry [DataTable] (e.g. it is not
	// partial / is owned elsewhere). [DataTableIndex] / [DataTableSort] go on the annotated class.
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	internal sealed class GenerateDataTableStoreAttribute : Attribute
	{
		public Type Target;
		public string[] UniqueKeys;
		public GenerateDataTableStoreAttribute(Type target, params string[] uniqueKeys)
		{
			Target = target;
			UniqueKeys = uniqueKeys;
		}
	}
}
");

				ctx.AddSource("GenerateAggregateStoreAttribute.cs", /* lang=C#-test, lang=C# */ @"
using System;

namespace DataTableGenerator
{
	// Put this on a partial class to generate SetData/UpdateData over an aggregate record:
	// each array property composes the DataTable store of its element type, each property whose type
	// has its own [GenerateAggregateStore] composes that nested store, and everything else is held
	// whole. SetData overwrites; UpdateData applies only the non-null members of a delta.
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	internal sealed class GenerateAggregateStoreAttribute : Attribute
	{
		public Type Target;
		public GenerateAggregateStoreAttribute(Type target)
		{
			Target = target;
		}
	}
}
");
			});

			var source = context.SyntaxProvider.ForAttributeWithMetadataName(
				"DataTableGenerator.DataTableAttribute", static (node, token) => true,
				Transform).WithTrackingName("DataTableGenerator.DataTable");
			context.RegisterSourceOutput(source, Emit);

			var targeted = context.SyntaxProvider.ForAttributeWithMetadataName(
				"DataTableGenerator.GenerateDataTableStoreAttribute", static (node, token) => true,
				TransformTargeted).WithTrackingName("DataTableGenerator.GenerateDataTableStore");
			context.RegisterSourceOutput(targeted, Emit);

			// Registry of "type -> store class" for every store-producing declaration in the compilation,
			// so an aggregate can resolve which store to compose for each member type. SetData/UpdateData
			// are fixed names, so the class name is all that is needed.
			var inPlaceStores = context.SyntaxProvider.ForAttributeWithMetadataName(
				"DataTableGenerator.DataTableAttribute", static (node, _) => true, StoreRefInPlace);
			var targetedStores = context.SyntaxProvider.ForAttributeWithMetadataName(
				"DataTableGenerator.GenerateDataTableStoreAttribute", static (node, _) => true, StoreRefTargeted);
			var aggregateStores = context.SyntaxProvider.ForAttributeWithMetadataName(
				"DataTableGenerator.GenerateAggregateStoreAttribute", static (node, _) => true, StoreRefAggregate);

			var registry = inPlaceStores.Collect()
				.Combine(targetedStores.Collect())
				.Combine(aggregateStores.Collect())
				.Select(static (t, _) => t.Left.Left.AddRange(t.Left.Right).AddRange(t.Right));

			var aggregates = context.SyntaxProvider.ForAttributeWithMetadataName(
				"DataTableGenerator.GenerateAggregateStoreAttribute", static (node, _) => node is not null,
				TransformAggregate).WithTrackingName("DataTableGenerator.GenerateAggregateStore");

			context.RegisterSourceOutput(aggregates.Combine(registry), EmitAggregate);
		}

		static GeneratorSource TransformTargeted(GeneratorAttributeSyntaxContext context,
			CancellationToken cancellationToken)
		{
			var storeClass = (INamedTypeSymbol)context.TargetSymbol;
			var attr = storeClass.FindAttribute("DataTableGenerator", "GenerateDataTableStoreAttribute");
			var rowType = attr.ConstructorArguments[0].Value as INamedTypeSymbol;
			var keys = attr.ConstructorArguments.Length > 1
				? attr.ConstructorArguments[1].Values.Select(static v => v.Value as string).ToArray()
				: Array.Empty<string>();
			var uniqueKey = new IndexSource(attr, keys);
			var indexList = new List<IndexSource>();
			foreach (var a in storeClass.FindAttributes("DataTableGenerator", "DataTableIndexAttribute"))
			{
				indexList.Add(new IndexSource(a));
			}
			var sortList = new List<SortSource>();
			foreach (var a in storeClass.FindAttributes("DataTableGenerator", "DataTableSortAttribute"))
			{
				sortList.Add(new SortSource(a));
			}
			var comparerList = new List<SortComparerSource>();
			foreach (var a in storeClass.FindAttributes("DataTableGenerator", "DataTableSortComparerAttribute"))
			{
				comparerList.Add(new SortComparerSource(a));
			}
			return new GeneratorSource(context, rowType, storeClass, uniqueKey, indexList.ToArray(),
				sortList.ToArray(), comparerList.ToArray());
		}

		// ---- aggregate store -----------------------------------------------------------------

		static readonly DiagnosticDescriptor MissingStoreDescriptor = new(
			"DataTableGenerator017", "Aggregate store",
			"No store found for type '{0}' (array member '{1}'). Declare [GenerateDataTableStore(typeof({0}), ...)].",
			"SourceGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

		static StoreRef StoreRefInPlace(GeneratorAttributeSyntaxContext context, CancellationToken token)
		{
			var c = (INamedTypeSymbol)context.TargetSymbol;
			return new StoreRef(c.QualifiedName(), c.QualifiedName() + "Store");
		}

		static StoreRef StoreRefTargeted(GeneratorAttributeSyntaxContext context, CancellationToken token)
		{
			var store = (INamedTypeSymbol)context.TargetSymbol;
			var attr = store.FindAttribute("DataTableGenerator", "GenerateDataTableStoreAttribute");
			var row = attr.ConstructorArguments[0].Value as INamedTypeSymbol;
			return new StoreRef(row?.QualifiedName() ?? "", store.QualifiedName());
		}

		static StoreRef StoreRefAggregate(GeneratorAttributeSyntaxContext context, CancellationToken token)
		{
			var store = (INamedTypeSymbol)context.TargetSymbol;
			var attr = store.FindAttribute("DataTableGenerator", "GenerateAggregateStoreAttribute");
			var target = attr.ConstructorArguments[0].Value as INamedTypeSymbol;
			return new StoreRef(target?.QualifiedName() ?? "", store.QualifiedName());
		}

		static AggregateModel TransformAggregate(GeneratorAttributeSyntaxContext context,
			CancellationToken cancellationToken)
		{
			var store = (INamedTypeSymbol)context.TargetSymbol;
			var ns = store.ContainingNamespace.IsGlobalNamespace ? null : store.ContainingNamespace.ToString();
			var attr = store.FindAttribute("DataTableGenerator", "GenerateAggregateStoreAttribute");
			var target = attr.ConstructorArguments[0].Value as INamedTypeSymbol;

			var members = new List<AggregateMember>();
			if (target != null)
			{
				foreach (var member in target.GetMembers())
				{
					ITypeSymbol type;
					string name;
					if (member is IPropertySymbol { DeclaredAccessibility: Accessibility.Public, IsStatic: false, IsIndexer: false } p)
					{
						type = p.Type;
						name = p.Name;
					}
					else if (member is IFieldSymbol { DeclaredAccessibility: Accessibility.Public, IsStatic: false } f)
					{
						type = f.Type;
						name = f.Name;
					}
					else
					{
						continue;
					}

					// Array members are delegated to a store, so SetData rejects null for them. Held-whole
					// members keep the record's nullability: a nullable member may legitimately be null, a
					// non-nullable reference member is unset until SetData and so gets `= null!`.
					// Store lookup goes through the unannotated name so that `T?` resolves to the store
					// registered for `T`. An annotated element type (`Row?[]`) also resolves to the store for `Row`;
					// the generated SetData / UpdateData then reject null elements.
					var isNonNullableReference = type.IsReferenceType &&
						type.NullableAnnotation != NullableAnnotation.Annotated;
					var unannotated = type.IsReferenceType
						? type.WithNullableAnnotation(NullableAnnotation.NotAnnotated)
						: type;
					if (unannotated is IArrayTypeSymbol array)
					{
						var element = array.ElementType;
						var elementIsNullable = element.IsReferenceType &&
							element.NullableAnnotation == NullableAnnotation.Annotated;
						var elementKey = element.IsReferenceType
							? element.WithNullableAnnotation(NullableAnnotation.NotAnnotated).QualifiedName()
							: element.QualifiedName();
						members.Add(new AggregateMember(name, type.QualifiedName(), elementKey,
							isArray: true, guarded: true, isNonNullableReference: isNonNullableReference,
							elementIsNullable: elementIsNullable));
					}
					else
					{
						var isNullableValue = type is INamedTypeSymbol named &&
							named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
						var guarded = type.IsReferenceType || isNullableValue;
						members.Add(new AggregateMember(name, type.QualifiedName(), unannotated.QualifiedName(),
							isArray: false, guarded: guarded, isNonNullableReference: isNonNullableReference,
							elementIsNullable: false));
					}
				}
			}

			return new AggregateModel(ns, store.Name, target?.QualifiedName() ?? "", members.ToArray());
		}

		static void EmitAggregate(SourceProductionContext context,
			(AggregateModel Model, ImmutableArray<StoreRef> Registry) input)
		{
			var model = input.Model;
			var map = new Dictionary<string, string>();
			foreach (var store in input.Registry)
			{
				if (store != null && store.TypeFqn.Length > 0 && !map.ContainsKey(store.TypeFqn))
				{
					map[store.TypeFqn] = store.StoreClassFqn;
				}
			}

			using var _ = StringBuilderHolder.Get(out var sb);
			sb.Append(GeneratedFileHeader);
			sb.AppendLine("using System.Collections.Generic;");
			sb.AppendLine("using System.Linq;");
			sb.AppendLine();

			var indent = "";
			if (model.Namespace != null)
			{
				sb.AppendLine($"namespace {model.Namespace}");
				sb.AppendLine("{");
				indent = "\t";
			}
			var m = indent + "\t";
			var mm = m + "\t";

			sb.AppendLine($"{indent}public partial class {model.ClassName}");
			sb.AppendLine($"{indent}{{");

			foreach (var member in model.Members)
			{
				if (map.TryGetValue(member.LookupFqn, out var store))
				{
					sb.AppendLine($"{m}public {store} {member.Name} {{ get; }} = new();");
				}
				else if (member.IsArray)
				{
					context.ReportDiagnostic(Diagnostic.Create(MissingStoreDescriptor, Location.None,
						member.LookupFqn, member.Name));
				}
				else
				{
					var initializer = member.IsNonNullableReference ? " = null!;" : "";
					sb.AppendLine($"{m}public {member.MemberTypeFqn} {member.Name} {{ get; private set; }}{initializer}");
				}
			}

			sb.AppendLine($"{m}public void SetData({model.TargetFqn} data)");
			sb.AppendLine($"{m}{{");
			foreach (var member in model.Members)
			{
				if (map.ContainsKey(member.LookupFqn))
				{
					if (member.ElementIsNullable)
					{
						sb.AppendLine($"{mm}{member.Name}.SetData((data.{member.Name} ?? " +
							$"throw new global::System.ArgumentNullException(nameof(data.{member.Name})))" +
							$".Select(x => x ?? throw new global::System.ArgumentNullException(nameof(data.{member.Name}))));");
					}
					else
					{
						sb.AppendLine($"{mm}{member.Name}.SetData(data.{member.Name} ?? " +
							$"throw new global::System.ArgumentNullException(nameof(data.{member.Name})));");
					}
				}
				else if (!member.IsArray)
				{
					sb.AppendLine($"{mm}{member.Name} = data.{member.Name};");
				}
			}
			sb.AppendLine($"{m}}}");

			sb.AppendLine($"{m}public void UpdateData({model.TargetFqn} data)");
			sb.AppendLine($"{m}{{");
			foreach (var member in model.Members)
			{
				if (map.ContainsKey(member.LookupFqn))
				{
					var updateArgument = member.ElementIsNullable
						? $"data.{member.Name}.Select(x => x ?? throw new global::System.ArgumentNullException(nameof(data.{member.Name})))"
						: $"data.{member.Name}";
					sb.AppendLine($"{mm}if (data.{member.Name} != null) {member.Name}.UpdateData({updateArgument});");
				}
				else if (!member.IsArray)
				{
					if (member.Guarded)
					{
						sb.AppendLine($"{mm}if (data.{member.Name} != null) {member.Name} = data.{member.Name};");
					}
					else
					{
						sb.AppendLine($"{mm}{member.Name} = data.{member.Name};");
					}
				}
			}
			sb.AppendLine($"{m}}}");

			sb.AppendLine($"{indent}}}");
			if (model.Namespace != null)
			{
				sb.AppendLine("}");
			}

			context.AddSource($"{model.Namespace ?? "(global)"} {model.ClassName}.Aggregate.cs", sb.ToString());
		}

		internal sealed class StoreRef : IEquatable<StoreRef>
		{
			public string TypeFqn { get; }
			public string StoreClassFqn { get; }

			public StoreRef(string typeFqn, string storeClassFqn)
			{
				TypeFqn = typeFqn;
				StoreClassFqn = storeClassFqn;
			}

			public bool Equals(StoreRef other) =>
				other != null && TypeFqn == other.TypeFqn && StoreClassFqn == other.StoreClassFqn;

			public override bool Equals(object obj) => obj is StoreRef s && Equals(s);
			public override int GetHashCode() => (TypeFqn?.GetHashCode() ?? 0) ^ (StoreClassFqn?.GetHashCode() ?? 0);
		}

		internal sealed class AggregateMember : IEquatable<AggregateMember>
		{
			public string Name { get; }
			public string MemberTypeFqn { get; }
			public string LookupFqn { get; }
			public bool IsArray { get; }
			public bool Guarded { get; }
			public bool IsNonNullableReference { get; }
			public bool ElementIsNullable { get; }

			public AggregateMember(string name, string memberTypeFqn, string lookupFqn, bool isArray, bool guarded,
				bool isNonNullableReference, bool elementIsNullable)
			{
				Name = name;
				MemberTypeFqn = memberTypeFqn;
				LookupFqn = lookupFqn;
				IsArray = isArray;
				Guarded = guarded;
				IsNonNullableReference = isNonNullableReference;
				ElementIsNullable = elementIsNullable;
			}

			public bool Equals(AggregateMember other) =>
				other != null && Name == other.Name && MemberTypeFqn == other.MemberTypeFqn &&
				LookupFqn == other.LookupFqn && IsArray == other.IsArray && Guarded == other.Guarded &&
				IsNonNullableReference == other.IsNonNullableReference && ElementIsNullable == other.ElementIsNullable;

			public override bool Equals(object obj) => obj is AggregateMember a && Equals(a);

			public override int GetHashCode()
			{
				unchecked
				{
					var hash = 17;
					hash = hash * 31 + (Name?.GetHashCode() ?? 0);
					hash = hash * 31 + (MemberTypeFqn?.GetHashCode() ?? 0);
					hash = hash * 31 + (LookupFqn?.GetHashCode() ?? 0);
					hash = hash * 31 + IsArray.GetHashCode();
					hash = hash * 31 + Guarded.GetHashCode();
					hash = hash * 31 + IsNonNullableReference.GetHashCode();
					hash = hash * 31 + ElementIsNullable.GetHashCode();
					return hash;
				}
			}
		}

		internal sealed class AggregateModel : IEquatable<AggregateModel>
		{
			public string Namespace { get; }
			public string ClassName { get; }
			public string TargetFqn { get; }
			public AggregateMember[] Members { get; }

			public AggregateModel(string ns, string className, string targetFqn, AggregateMember[] members)
			{
				Namespace = ns;
				ClassName = className;
				TargetFqn = targetFqn;
				Members = members;
			}

			public bool Equals(AggregateModel other) =>
				other != null && Namespace == other.Namespace && ClassName == other.ClassName &&
				TargetFqn == other.TargetFqn && Members.AsSpan().SequenceEqual(other.Members.AsSpan());

			public override bool Equals(object obj) => obj is AggregateModel a && Equals(a);

			public override int GetHashCode()
			{
				unchecked
				{
					var hash = Namespace?.GetHashCode() ?? 0;
					hash = hash * 31 + (ClassName?.GetHashCode() ?? 0);
					hash = hash * 31 + (TargetFqn?.GetHashCode() ?? 0);
					foreach (var member in Members) hash = hash * 31 + member.GetHashCode();
					return hash;
				}
			}
		}

		static GeneratorSource Transform(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
		{
			var targetSymbol = (INamedTypeSymbol)context.TargetSymbol;
			var uniqueKey = new IndexSource(targetSymbol.FindAttribute("DataTableGenerator", "DataTableAttribute"));
			var indexList = new List<IndexSource>();
			foreach (var attr in targetSymbol.FindAttributes("DataTableGenerator", "DataTableIndexAttribute"))
			{
				indexList.Add(new IndexSource(attr));
			}
			var sortList = new List<SortSource>();
			foreach (var attr in targetSymbol.FindAttributes("DataTableGenerator", "DataTableSortAttribute"))
			{
				sortList.Add(new SortSource(attr));
			}
			var comparerList = new List<SortComparerSource>();
			foreach (var attr in targetSymbol.FindAttributes("DataTableGenerator", "DataTableSortComparerAttribute"))
			{
				comparerList.Add(new SortComparerSource(attr));
			}
			return new GeneratorSource(context, targetSymbol, uniqueKey, indexList.ToArray(), sortList.ToArray(), comparerList.ToArray());
		}

		static void Emit(SourceProductionContext context, GeneratorSource source)
		{
			GeneratorHelper.Run(context, source, $"{source.Namespace ?? "(global)"} {source.Name}.cs",
				static (ctx, src, sb) =>
				{
					var members = new Dictionary<string, (ISymbol symbol, string qualifiedTypeName)>();
					(string type, string[] names) uniqueKey;
					var indexList = new List<(string type, string[] names, string combinedName)>();
					var sortList = new List<SortSource>();

					if (src.UniqueKeySource.Names.Length == 0)
					{
						Errors.UniqueKeysRequired(ctx, src.Target, src.UniqueKeySource.Attribute);
						throw new Exception();
					}
					else if (Array.Exists(src.UniqueKeySource.Names, static x => x == null))
					{
						Errors.UniqueKeysMustBeString(ctx, src.Target, src.UniqueKeySource.Attribute);
						throw new Exception();
					}
					try
					{
						var typeString = src.UniqueKeySource.ToTypeString(src, members);
						uniqueKey = (typeString, src.UniqueKeySource.Names);
					}
					catch (IndexSourceConversionException ex)
					{
						switch (ex.ErrorCode)
						{
							case IndexSourceConversionException.Error.InvalidIndexName:
								Errors.InvalidIndexName(ctx, src.Target, src.UniqueKeySource.Attribute, ex.Name);
								break;
							case IndexSourceConversionException.Error.IndexNameMustBePropertyOrField:
								Errors.IndexNameMustBePropertyOrField(ctx, src.Target, src.UniqueKeySource.Attribute,
									ex.Name);
								break;
						}
						throw;
					}

					foreach (var index in src.IndexSources)
					{
						if (index.Names.Length == 0)
						{
							Errors.IndexNamesRequired(ctx, src.Target, index.Attribute);
							throw new Exception();
						}
						else if (Array.Exists(index.Names, static x => x == null))
						{
							Errors.IndexNamesMustBeString(ctx, src.Target, index.Attribute);
							throw new Exception();
						}

						try
						{
							var typeString = index.ToTypeString(src, members);
							indexList.Add((typeString, index.Names, string.Join("And", index.Names)));
						}
						catch (IndexSourceConversionException ex)
						{
							switch (ex.ErrorCode)
							{
								case IndexSourceConversionException.Error.InvalidIndexName:
									Errors.InvalidIndexName(ctx, src.Target, index.Attribute, ex.Name);
									break;
								case IndexSourceConversionException.Error.IndexNameMustBePropertyOrField:
									Errors.IndexNameMustBePropertyOrField(ctx, src.Target, index.Attribute, ex.Name);
									break;
							}
							throw;
						}
					}

					foreach (var sort in src.SortSources)
					{
						if (sort.Keys.Length == 0)
						{
							Errors.SortKeysRequired(ctx, src.Target, sort.Attribute);
							throw new Exception();
						}
						else if (Array.Exists(sort.Keys, static x => x.Name == null))
						{
							Errors.SortKeysMustBeString(ctx, src.Target, sort.Attribute);
							throw new Exception();
						}

						try
						{
							sort.Validate(src, members);
							sortList.Add(sort);
						}
						catch (IndexSourceConversionException ex)
						{
							switch (ex.ErrorCode)
							{
								case IndexSourceConversionException.Error.InvalidIndexName:
									Errors.InvalidSortKeyName(ctx, src.Target, sort.Attribute, ex.Name);
									break;
								case IndexSourceConversionException.Error.IndexNameMustBePropertyOrField:
									Errors.SortKeyMustBePropertyOrField(ctx, src.Target, sort.Attribute, ex.Name);
									break;
							}
							throw;
						}
					}

					sb.Append(GeneratedFileHeader);
					sb.AppendLine(@"using System.Collections.Generic;
using System.Linq;
");

					if (src.Namespace != null)
					{
						sb.AppendLine(@$"namespace {src.Namespace}
{{");
					}

					sb.AppendLine(@$"	/// <summary>
	/// Auto-generated data store for <see cref=""{src.QualifiedName}""/>.
	/// </summary>
	public partial class {src.StoreClassName}
	{{
		Dictionary<{uniqueKey.type}, {src.QualifiedName}> UniqueIndexDictionary = new();
		public IReadOnlyDictionary<{uniqueKey.type}, {src.QualifiedName}> UniqueIndex => UniqueIndexDictionary;
		List<{src.QualifiedName}> AllDataList = new();
		public IReadOnlyList<{src.QualifiedName}> AllData => AllDataList;");

					foreach (var index in indexList)
					{
						sb.AppendLine(
							@$"		Dictionary<{index.type}, {src.QualifiedName}> {index.combinedName}Dictionary = new();
		public IReadOnlyDictionary<{index.type}, {src.QualifiedName}> {index.combinedName}Index => {index.combinedName}Dictionary;");
					}

					foreach (var sort in sortList)
					{
						sb.AppendLine(@$"		List<{src.QualifiedName}> SortedBy{sort.CombinedName}List = new();
		public IReadOnlyList<{src.QualifiedName}> SortedBy{sort.CombinedName} => SortedBy{sort.CombinedName}List;");
					}

					sb.AppendLine(@$"		partial void OnDataSet();
		partial void OnDataUpdated();
		partial void OnDataRemoved();
		public void SetData(IEnumerable<{src.QualifiedName}> data)
		{{");

					var dataVar = "data";
					if (sortList.Count > 0)
					{
						sb.AppendLine($"			var dataList = data.ToList();");
						dataVar = "dataList";
					}

					sb.AppendLine($"			UniqueIndexDictionary.Clear();");
					foreach (var index in indexList)
					{
						sb.AppendLine($"			{index.combinedName}Dictionary.Clear();");
					}
					sb.AppendLine(@$"			foreach (var d in {dataVar})
			{{");
					if (uniqueKey.names.Length == 1)
					{
						sb.AppendLine($"				UniqueIndexDictionary.Add(d.{uniqueKey.names[0]}, d);");
					}
					else
					{
						sb.AppendLine(
							$"				UniqueIndexDictionary.Add(({string.Join(", ", uniqueKey.names.Select(static x => "d." + x))}), d);");
					}
					foreach (var index in indexList)
					{
						if (index.names.Length == 1)
						{
							sb.AppendLine($"				{index.combinedName}Dictionary.Add(d.{index.names[0]}, d);");
						}
						else
						{
							sb.AppendLine(
								$"				{index.combinedName}Dictionary.Add(({string.Join(", ", index.names.Select(static x => "d." + x))}), d);");
						}
					}
					sb.AppendLine(@"			}");

					var comparerMap = new Dictionary<string, string>();
					foreach (var c in src.SortComparerSources)
					{
						if (c.Key == null)
						{
							Errors.SortComparerKeyRequired(ctx, src.Target, c.Attribute);
							throw new Exception();
						}
						if (!members.ContainsKey(c.Key))
						{
							var m = src.Target.GetMembers(c.Key);
							if (m.Length == 0)
							{
								Errors.InvalidSortComparerKeyName(ctx, src.Target, c.Attribute, c.Key);
								throw new Exception();
							}
							switch (m[0])
							{
								case IFieldSymbol field:
									members[c.Key] = (field, field.Type.QualifiedName());
									break;
								case IPropertySymbol property:
									members[c.Key] = (property, property.Type.QualifiedName());
									break;
								default:
									Errors.SortComparerKeyMustBePropertyOrField(ctx, src.Target, c.Attribute, c.Key);
									throw new Exception();
							}
						}
						if (c.ComparerTypeQualifiedName != null)
						{
							comparerMap[c.Key] = c.ComparerTypeQualifiedName;
						}
					}

					string ComparerArg(string keyName)
					{
						return comparerMap.TryGetValue(keyName, out var t) ? $", new {t}()" : "";
					}

					foreach (var sort in sortList)
					{
						using var __ = StringBuilderHolder.Get(out var linq);
						var firstKey = sort.Keys[0];
						var firstArg = ComparerArg(firstKey.Name);
						linq.Append(firstKey.Descending
							? $"OrderByDescending(d => d.{firstKey.Name}{firstArg})"
							: $"OrderBy(d => d.{firstKey.Name}{firstArg})");
						for (var i = 1; i < sort.Keys.Length; i++)
						{
							var key = sort.Keys[i];
							var arg = ComparerArg(key.Name);
							linq.Append(key.Descending
								? $".ThenByDescending(d => d.{key.Name}{arg})"
								: $".ThenBy(d => d.{key.Name}{arg})");
						}
						sb.AppendLine($"			SortedBy{sort.CombinedName}List = {dataVar}.{linq}.ToList();");
					}

					sb.AppendLine(@"			AllDataList = UniqueIndexDictionary.Values.ToList();
			OnDataSet();
		}");


					// UpdateData / RemoveData は unique 辞書だけを増分更新し、index 辞書・ソート済みリスト・
					// AllDataList は unique 辞書の Values から作り直す。増分更新だと、更新で index キー値が
					// 変わった行の旧エントリやソート済みリスト内の旧インスタンスが残るため。
					void EmitRebuild()
					{
						if (indexList.Count > 0)
						{
							foreach (var index in indexList)
							{
								sb.AppendLine($"			{index.combinedName}Dictionary.Clear();");
							}
							sb.AppendLine(@"			foreach (var d in UniqueIndexDictionary.Values)
			{");
							foreach (var index in indexList)
							{
								if (index.names.Length == 1)
								{
									sb.AppendLine($"				{index.combinedName}Dictionary[d.{index.names[0]}] = d;");
								}
								else
								{
									sb.AppendLine(
										$"				{index.combinedName}Dictionary[({string.Join(", ", index.names.Select(static x => "d." + x))})] = d;");
								}
							}
							sb.AppendLine("			}");
						}
						foreach (var sort in sortList)
						{
							using var __ = StringBuilderHolder.Get(out var linq);
							var firstKey = sort.Keys[0];
							var firstArg = ComparerArg(firstKey.Name);
							linq.Append(firstKey.Descending
								? $"OrderByDescending(d => d.{firstKey.Name}{firstArg})"
								: $"OrderBy(d => d.{firstKey.Name}{firstArg})");
							for (var i = 1; i < sort.Keys.Length; i++)
							{
								var key = sort.Keys[i];
								var arg = ComparerArg(key.Name);
								linq.Append(key.Descending
									? $".ThenByDescending(d => d.{key.Name}{arg})"
									: $".ThenBy(d => d.{key.Name}{arg})");
							}
							sb.AppendLine(
								$"			SortedBy{sort.CombinedName}List = UniqueIndexDictionary.Values.{linq}.ToList();");
						}
						sb.AppendLine("			AllDataList = UniqueIndexDictionary.Values.ToList();");
					}

					sb.AppendLine(@$"		public void UpdateData(IEnumerable<{src.QualifiedName}> data)
		{{
			foreach (var d in data)
			{{");
					if (uniqueKey.names.Length == 1)
					{
						sb.AppendLine($"				UniqueIndexDictionary[d.{uniqueKey.names[0]}] = d;");
					}
					else
					{
							sb.AppendLine($"                UniqueIndexDictionary[({string.Join(", ", uniqueKey.names.Select(static x => "d." + x))})] = d;");
					}
					sb.AppendLine(@"			}");
					EmitRebuild();
					sb.AppendLine(@"			OnDataUpdated();
		}");

					sb.AppendLine(@$"		public void RemoveData(IEnumerable<{uniqueKey.type}> keys)
		{{
			foreach (var key in keys)
			{{
				UniqueIndexDictionary.Remove(key);
			}}");
					EmitRebuild();
					sb.AppendLine(@"			OnDataRemoved();
		}
	}");

					if (src.Namespace != null)
					{
						sb.AppendLine("}");
					}
				});
		}
	}
}