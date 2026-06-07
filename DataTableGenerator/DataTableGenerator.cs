using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("DataTableGeneratorTest")]

namespace DataTableGenerator
{
	[Generator(LanguageNames.CSharp)]
	public class DataTableGenerator : IIncrementalGenerator
	{
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
			});

			var source = context.SyntaxProvider.ForAttributeWithMetadataName(
				"DataTableGenerator.DataTableAttribute", static (node, token) => true,
				Transform).WithTrackingName("DataTableGenerator.DataTable");
			context.RegisterSourceOutput(source, Emit);
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
			return new GeneratorSource(context, targetSymbol, uniqueKey, indexList.ToArray(), sortList.ToArray());
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
	public partial class {src.Name}Store
	{{
		Dictionary<{uniqueKey.type}, {src.QualifiedName}> UniqueIndexDictionary = new();
		public IReadOnlyDictionary<{uniqueKey.type}, {src.QualifiedName}> UniqueIndex => UniqueIndexDictionary;");

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

					foreach (var sort in sortList)
					{
						using var __ = StringBuilderHolder.Get(out var linq);
						var firstKey = sort.Keys[0];
						linq.Append(firstKey.Descending
							? $"OrderByDescending(d => d.{firstKey.Name})"
							: $"OrderBy(d => d.{firstKey.Name})");
						for (var i = 1; i < sort.Keys.Length; i++)
						{
							var key = sort.Keys[i];
							linq.Append(key.Descending
								? $".ThenByDescending(d => d.{key.Name})"
								: $".ThenBy(d => d.{key.Name})");
						}
						sb.AppendLine($"			SortedBy{sort.CombinedName}List = {dataVar}.{linq}.ToList();");
					}

					sb.AppendLine(@"			OnDataSet();
		}");


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
						sb.AppendLine(
							$"				UniqueIndexDictionary[({string.Join(", ", uniqueKey.names.Select(static x => "d." + x))}] = d;");
					}
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
					sb.AppendLine(@"			}
			OnDataUpdated();
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