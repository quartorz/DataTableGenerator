using System;
using System.Text;
using Microsoft.CodeAnalysis;

namespace DataTableGenerator
{
	internal static class GeneratorHelper
	{
		public static void Run<T>(SourceProductionContext context, T source, string hintName,
			Action<SourceProductionContext, T, StringBuilder> action)
		{
			using var _ = StringBuilderHolder.Get(out var builder);
			try
			{
				action(context, source, builder);
			}
			catch (Exception e)
			{
				Errors.Unexpected(context, e);
			}
			context.AddSource(hintName, builder.ToString());
		}
	}
}