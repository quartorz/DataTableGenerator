using Microsoft.CodeAnalysis;
using Xunit;

namespace DataTableGeneratorTest;

public class DataTableGenerator_Errors
{
	[Fact]
	public void IndexNames_Empty()
	{
		var generator = new DataTableGenerator.DataTableGenerator();
		
		/* lang=C#-test, lang=C# */
		var source = @"
[DataTableGenerator.DataTable]
[DataTableGenerator.DataTableIndex()]
class Data
{
}
";
		
		var r = GeneratorRunner.RunAndFilter(generator.AsSourceGenerator(), "DataTableGenerator", source);
		Assert.NotNull(r.Diagnostics.Find(static x => x.Descriptor.Id == "DataTableGenerator002"));
	}
	
	[Fact]
	public void IndexNames_Null()
	{
		var generator = new DataTableGenerator.DataTableGenerator();
		
		/* lang=C#-test, lang=C# */
		var source = @"
[DataTableGenerator.DataTable]
[DataTableGenerator.DataTableIndex(""Id"", null)]
class Data
{
}
";
		
		var r = GeneratorRunner.RunAndFilter(generator.AsSourceGenerator(), "DataTableGenerator", source);
		Assert.NotNull(r.Diagnostics.Find(static x => x.Descriptor.Id == "DataTableGenerator003"));
	}
	
	[Fact]
	public void IndexNames_NotExists()
	{
		var generator = new DataTableGenerator.DataTableGenerator();
		
		/* lang=C#-test, lang=C# */
		var source = @"
[DataTableGenerator.DataTable]
[DataTableGenerator.DataTableIndex(""Id"")]
class Data
{
}
";
		
		var r = GeneratorRunner.RunAndFilter(generator.AsSourceGenerator(), "DataTableGenerator", source);
		Assert.NotNull(r.Diagnostics.Find(static x => x.Descriptor.Id == "DataTableGenerator004"));
	}
	
	[Fact]
	public void IndexNames_MethodName()
	{
		var generator = new DataTableGenerator.DataTableGenerator();
		
		/* lang=C#-test, lang=C# */
		var source = @"
[DataTableGenerator.DataTable]
[DataTableGenerator.DataTableIndex(""Id"")]
class Data
{
	public void Id() { }
}
";
		
		var r = GeneratorRunner.RunAndFilter(generator.AsSourceGenerator(), "DataTableGenerator", source);
		Assert.NotNull(r.Diagnostics.Find(static x => x.Descriptor.Id == "DataTableGenerator005"));
	}
}