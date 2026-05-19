using System;
using Microsoft.CodeAnalysis;

namespace DataTableGenerator
{
	internal static class Errors
	{
		public static void Unexpected(SourceProductionContext context, Exception exception)
		{
			var diagnostic = Diagnostic.Create(new DiagnosticDescriptor(id: "DataTableGenerator001",
					title: "DataTableGenerator Unexpected Error", messageFormat: "An error occurred: {0}",
					category: "SourceGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true), Location.None,
				exception.Message);
			context.ReportDiagnostic(diagnostic);
		}

		public static void IndexNamesRequired(SourceProductionContext context, INamedTypeSymbol target,
			AttributeData attribute)
		{
			var diagnostic = Diagnostic.Create(
				new DiagnosticDescriptor(id: "DataTableGenerator002", title: "Index Names are Required",
					messageFormat: "Index names are required for {0}", category: "SourceGenerator",
					DiagnosticSeverity.Error, isEnabledByDefault: true),
				attribute.ApplicationSyntaxReference!.SyntaxTree.GetLocation(attribute.ApplicationSyntaxReference.Span),
				target.QualifiedName());
			context.ReportDiagnostic(diagnostic);
		}

		public static void IndexNamesMustBeString(SourceProductionContext context, INamedTypeSymbol target,
			AttributeData attribute)
		{
			var diagnostic = Diagnostic.Create(
				new DiagnosticDescriptor(id: "DataTableGenerator003", title: "Index Name must be String",
					messageFormat: "Index names for {0} must all be strings", category: "SourceGenerator",
					DiagnosticSeverity.Error, isEnabledByDefault: true),
				attribute.ApplicationSyntaxReference!.SyntaxTree.GetLocation(attribute.ApplicationSyntaxReference.Span),
				target.QualifiedName());
			context.ReportDiagnostic(diagnostic);
		}

		public static void InvalidIndexName(SourceProductionContext context, INamedTypeSymbol target,
			AttributeData attribute, string name)
		{
			var diagnostic = Diagnostic.Create(
				new DiagnosticDescriptor(id: "DataTableGenerator004", title: "Invalid Index Name",
					messageFormat: "The name '{0}' is not a valid member name of {1}", category: "SourceGenerator",
					DiagnosticSeverity.Error, isEnabledByDefault: true),
				attribute.ApplicationSyntaxReference!.SyntaxTree.GetLocation(attribute.ApplicationSyntaxReference.Span),
				name, target.QualifiedName());
			context.ReportDiagnostic(diagnostic);
		}

		public static void IndexNameMustBePropertyOrField(SourceProductionContext context, INamedTypeSymbol target,
			AttributeData attribute, string name)
		{
			var diagnostic = Diagnostic.Create(
				new DiagnosticDescriptor(id: "DataTableGenerator005", title: "Invalid Index Name",
					messageFormat: "'{0}' is not a property or field of {1}", category: "SourceGenerator",
					DiagnosticSeverity.Error, isEnabledByDefault: true),
				attribute.ApplicationSyntaxReference!.SyntaxTree.GetLocation(attribute.ApplicationSyntaxReference.Span),
				name, target.QualifiedName());
			context.ReportDiagnostic(diagnostic);
		}

		public static void UniqueKeysRequired(SourceProductionContext context, INamedTypeSymbol target,
			AttributeData attribute)
		{
			var diagnostic = Diagnostic.Create(
				new DiagnosticDescriptor(id: "DataTableGenerator006", title: "Unique Keys are Required",
					messageFormat: "Unique keys are required for {0}", category: "SourceGenerator",
					DiagnosticSeverity.Error, isEnabledByDefault: true),
				attribute.ApplicationSyntaxReference!.SyntaxTree.GetLocation(attribute.ApplicationSyntaxReference.Span),
				target.QualifiedName());
			context.ReportDiagnostic(diagnostic);
		}

		public static void UniqueKeysMustBeString(SourceProductionContext context, INamedTypeSymbol target,
			AttributeData attribute)
		{
			var diagnostic = Diagnostic.Create(
				new DiagnosticDescriptor(id: "DataTableGenerator007", title: "Unique Key must be String",
					messageFormat: "Unique keys for {0} must all be strings", category: "SourceGenerator",
					DiagnosticSeverity.Error, isEnabledByDefault: true),
				attribute.ApplicationSyntaxReference!.SyntaxTree.GetLocation(attribute.ApplicationSyntaxReference.Span),
				target.QualifiedName());
			context.ReportDiagnostic(diagnostic);
		}

		public static void InvalidUniqueKey(SourceProductionContext context, INamedTypeSymbol target,
			AttributeData attribute, string name)
		{
			var diagnostic = Diagnostic.Create(
				new DiagnosticDescriptor(id: "DataTableGenerator008", title: "Invalid Unique Key",
					messageFormat: "The name '{0}' is not a valid member name of {1}", category: "SourceGenerator",
					DiagnosticSeverity.Error, isEnabledByDefault: true),
				attribute.ApplicationSyntaxReference!.SyntaxTree.GetLocation(attribute.ApplicationSyntaxReference.Span),
				name, target.QualifiedName());
			context.ReportDiagnostic(diagnostic);
		}

		public static void UniqueKeyMustBePropertyOrField(SourceProductionContext context, INamedTypeSymbol target,
			AttributeData attribute, string name)
		{
			var diagnostic = Diagnostic.Create(
				new DiagnosticDescriptor(id: "DataTableGenerator009", title: "Invalid Unique Key",
					messageFormat: "'{0}' is not a property or field of {1}", category: "SourceGenerator",
					DiagnosticSeverity.Error, isEnabledByDefault: true),
				attribute.ApplicationSyntaxReference!.SyntaxTree.GetLocation(attribute.ApplicationSyntaxReference.Span),
				name, target.QualifiedName());
			context.ReportDiagnostic(diagnostic);
		}

		public static void SortKeysRequired(SourceProductionContext context, INamedTypeSymbol target,
			AttributeData attribute)
		{
			var diagnostic = Diagnostic.Create(
				new DiagnosticDescriptor(id: "DataTableGenerator010", title: "Sort Keys are Required",
					messageFormat: "Sort keys are required for {0}", category: "SourceGenerator",
					DiagnosticSeverity.Error, isEnabledByDefault: true),
				attribute.ApplicationSyntaxReference!.SyntaxTree.GetLocation(attribute.ApplicationSyntaxReference.Span),
				target.QualifiedName());
			context.ReportDiagnostic(diagnostic);
		}

		public static void SortKeysMustBeString(SourceProductionContext context, INamedTypeSymbol target,
			AttributeData attribute)
		{
			var diagnostic = Diagnostic.Create(
				new DiagnosticDescriptor(id: "DataTableGenerator011", title: "Sort Key must be String",
					messageFormat: "Sort keys for {0} must all be strings", category: "SourceGenerator",
					DiagnosticSeverity.Error, isEnabledByDefault: true),
				attribute.ApplicationSyntaxReference!.SyntaxTree.GetLocation(attribute.ApplicationSyntaxReference.Span),
				target.QualifiedName());
			context.ReportDiagnostic(diagnostic);
		}

		public static void InvalidSortKeyName(SourceProductionContext context, INamedTypeSymbol target,
			AttributeData attribute, string name)
		{
			var diagnostic = Diagnostic.Create(
				new DiagnosticDescriptor(id: "DataTableGenerator012", title: "Invalid Sort Key Name",
					messageFormat: "The name '{0}' is not a valid member name of {1}", category: "SourceGenerator",
					DiagnosticSeverity.Error, isEnabledByDefault: true),
				attribute.ApplicationSyntaxReference!.SyntaxTree.GetLocation(attribute.ApplicationSyntaxReference.Span),
				name, target.QualifiedName());
			context.ReportDiagnostic(diagnostic);
		}

		public static void SortKeyMustBePropertyOrField(SourceProductionContext context, INamedTypeSymbol target,
			AttributeData attribute, string name)
		{
			var diagnostic = Diagnostic.Create(
				new DiagnosticDescriptor(id: "DataTableGenerator013", title: "Invalid Sort Key Name",
					messageFormat: "'{0}' is not a property or field of {1}", category: "SourceGenerator",
					DiagnosticSeverity.Error, isEnabledByDefault: true),
				attribute.ApplicationSyntaxReference!.SyntaxTree.GetLocation(attribute.ApplicationSyntaxReference.Span),
				name, target.QualifiedName());
			context.ReportDiagnostic(diagnostic);
		}
	}
}