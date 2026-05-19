using Microsoft.CodeAnalysis;
using Xunit;

namespace DataTableGeneratorTest;

public class DataTableGenerator_Errors
{
	[Fact]
	public void UniqueKeys_Empty()
	{
		var generator = new DataTableGenerator.DataTableGenerator();
		
		/* lang=C#-test, lang=C# */
		var source = @"
[DataTableGenerator.DataTable()]
class Data
{
	public int Id { get; set; }
}
";
		
		var r = GeneratorRunner.RunAndFilter(generator.AsSourceGenerator(), "DataTableGenerator", source);
		Assert.NotNull(r.Diagnostics.Find(static x => x.Descriptor.Id == "DataTableGenerator006"));
	}
	
	[Fact]
	public void IndexNames_Empty()
	{
		var generator = new DataTableGenerator.DataTableGenerator();
		
		/* lang=C#-test, lang=C# */
		var source = @"
[DataTableGenerator.DataTable(""Id"")]
[DataTableGenerator.DataTableIndex()]
class Data
{
	public int Id { get; set; }
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
[DataTableGenerator.DataTable(""Id"")]
[DataTableGenerator.DataTableIndex(""Id"", null)]
class Data
{
	public int Id { get; set; }
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
[DataTableGenerator.DataTable(""Id"")]
[DataTableGenerator.DataTableIndex(""Id2"")]
class Data
{
	public int Id { get; set; }
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
[DataTableGenerator.DataTable(""Id"")]
[DataTableGenerator.DataTableIndex(""F"")]
class Data
{
	public int Id { get; set; }
	public void F() {}
}
";
		
		var r = GeneratorRunner.RunAndFilter(generator.AsSourceGenerator(), "DataTableGenerator", source);
		Assert.NotNull(r.Diagnostics.Find(static x => x.Descriptor.Id == "DataTableGenerator005"));
	}
}