using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DataTableGeneratorTest;

internal static class GeneratorRunner
{
	static readonly MetadataReference[] metadataReferences = new[]
	{
		MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(CSharpCompilation).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(Compilation).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(GCSettings).Assembly.Location),
	};

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