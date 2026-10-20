using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DataTableGeneratorTest;

internal static class GeneratorRunner
{
	static readonly string runtimeDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location)!;

	static readonly MetadataReference[] metadataReferences = new[]
	{
		MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(CSharpCompilation).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(Compilation).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(GCSettings).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
		MetadataReference.CreateFromFile(Path.Combine(runtimeDirectory, "System.Runtime.dll")),
		MetadataReference.CreateFromFile(Path.Combine(runtimeDirectory, "System.Collections.dll")),
	};

	/// <summary>
	/// Runs <paramref name="generator"/> over <paramref name="sources"/> with nullable reference types enabled
	/// for the whole compilation and compiles the result.
	/// </summary>
	/// <returns>
	/// The diagnostics of warning severity or above located in the generated syntax trees. Diagnostics in
	/// the input sources are left out, so the inputs need not be nullable-clean themselves.
	/// </returns>
	public static List<Diagnostic> CompileNullableAndCollectGeneratedDiagnostics(ISourceGenerator generator,
		params string[] sources)
	{
		var parseOptions = new CSharpParseOptions(LanguageVersion.Latest);
		var inputTrees = sources.Select(s => CSharpSyntaxTree.ParseText(s, parseOptions)).ToArray();
		var compilation = CSharpCompilation.Create("DataTableGeneratorTest", inputTrees, metadataReferences,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
				nullableContextOptions: NullableContextOptions.Enable));
		var driver = CSharpGeneratorDriver.Create(new[] {generator}).WithUpdatedParseOptions(parseOptions);
		driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);
		var inputTreeSet = new HashSet<SyntaxTree>(inputTrees);
		return outputCompilation.GetDiagnostics()
			.Where(d => d.Severity >= DiagnosticSeverity.Warning)
			.Where(d => d.Location.SourceTree is { } tree && !inputTreeSet.Contains(tree))
			.ToList();
	}

	public static (List<List<(string Key, List<IncrementalStepRunReason> Reasons)>> Steps, List<Diagnostic> Diagnostics,
		List<GeneratedSourceResult> Sources) Run(ISourceGenerator generator, params string[] sources)
	{
		var parsedOptions = new CSharpParseOptions(LanguageVersion.Latest);
		var driver = CSharpGeneratorDriver
			.Create(new[] {generator},
				driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None,
					trackIncrementalGeneratorSteps: true)).WithUpdatedParseOptions(parsedOptions);

		var compilation = CSharpCompilation
			.Create("DataTableGeneratorTest", options: new CSharpCompilationOptions(OutputKind.ConsoleApplication))
			.AddReferences(metadataReferences);
		var results = sources.Select(s =>
		{
			var c = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(s, parsedOptions));
			driver = driver.RunGenerators(c);
			var results = driver.GetRunResult().Results;
			return results[0];
		}).ToList();
		return (
			results.Select(static r => r.TrackedSteps.Select(static step => (step.Key,
				step.Value.SelectMany(static s => s.Outputs.Select(static o => o.Reason)).ToList())).ToList()).ToList(),
			results.SelectMany(static r => r.Diagnostics).ToList(),
			results.SelectMany(static r => r.GeneratedSources).ToList());
	}

	public static (List<List<(string Key, List<IncrementalStepRunReason> Reasons)>> Steps, List<Diagnostic> Diagnostics,
		List<GeneratedSourceResult> Sources) RunAndFilter(ISourceGenerator generator, string prefix,
			params string[] sources)
	{
		var result = Run(generator, sources);
		return (
			result.Steps.Select(r => r.Where(x => x.Key == "SourceOutput" || x.Key.StartsWith(prefix)).ToList())
				.ToList(), result.Diagnostics, result.Sources);
	}
}