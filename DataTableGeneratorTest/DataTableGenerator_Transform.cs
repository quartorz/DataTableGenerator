using Microsoft.CodeAnalysis;
using Xunit;

namespace DataTableGeneratorTest;

public class DataTableGenerator_Transform
{
	[Fact]
	public void Unchanged()
	{
		var generator = new DataTableGenerator.DataTableGenerator();
		
		/* lang=C#-test, lang=C# */
		var source1 = @"
[DataTableGenerator.DataTable]
[DataTableGenerator.DataTableIndex(""Id"")]
class Data
{
    public int Id { get; set; }
}
";
		/* lang=C#-test, lang=C# */
		var source2 = @"
[DataTableGenerator.DataTable]
[DataTableGenerator.DataTableIndex(""Id"")]
class Data
{
    public int Id { get; set; }
    public void Method() { }
}
";
		
		var r = GeneratorRunner.RunAndFilter(generator.AsSourceGenerator(), "DataTableGenerator", source1, source2);
		Assert.True(r.Steps[0].Find(static x => x.Key == "DataTableGenerator.DataTable").Reasons[0] is IncrementalStepRunReason.New);
		Assert.True(r.Steps[1].Find(static x => x.Key == "DataTableGenerator.DataTable").Reasons[0] is IncrementalStepRunReason.Unchanged or IncrementalStepRunReason.Cached);
	}
	
	[Fact]
	public void Changed()
	{
		var generator = new DataTableGenerator.DataTableGenerator();
		
		/* lang=C#-test, lang=C# */
		var source1 = @"
[DataTableGenerator.DataTable]
[DataTableGenerator.DataTableIndex(""Id"")]
class Data
{
}
";
		/* lang=C#-test, lang=C# */
		var source2 = @"
[DataTableGenerator.DataTable]
[DataTableGenerator.DataTableIndex(""Id"", ""Name"")]
class Data
{
    public void Method() { }
}
";
		
		var r = GeneratorRunner.RunAndFilter(generator.AsSourceGenerator(), "DataTableGenerator", source1, source2);
		Assert.True(r.Steps[0].Find(static x => x.Key == "DataTableGenerator.DataTable").Reasons[0] is IncrementalStepRunReason.New);
		Assert.True(r.Steps[1].Find(static x => x.Key == "DataTableGenerator.DataTable").Reasons[0] is IncrementalStepRunReason.Modified);
	}
}