using Microsoft.CodeAnalysis;
using Xunit;

namespace DataTableGeneratorTest;

public class DataTableGenerator_Success
{
	[Fact]
	public void NoIndexes()
	{
		var generator = new DataTableGenerator.DataTableGenerator();
		
		/* lang=C#-test, lang=C# */
		var source1 = @"
namespace N
{
	[DataTableGenerator.DataTable(""Id"")]
	class AData
	{
		public int Id { get; set; }
	}
}
";
		
		var r = GeneratorRunner.RunAndFilter(generator.AsSourceGenerator(), "DataTableGenerator", source1);
		Assert.Equal(/* lang=C#-test, lang=C# */ @"using System.Collections.Generic;
using System.Linq;

namespace N
{
	public partial class ADataStore
	{
		Dictionary<System.Int32, N.AData> UniqueIndexDictionary = new();
		public IReadOnlyDictionary<System.Int32, N.AData> UniqueIndex => UniqueIndexDictionary;
		public void SetData(IEnumerable<N.AData> data)
		{
			UniqueIndexDictionary.Clear();
			foreach (var d in data)
			{
				UniqueIndexDictionary.Add(d.Id, d);
			}
		}
		public void UpdateData(IEnumerable<N.AData> data)
		{
			foreach (var d in data)
			{
				UniqueIndexDictionary[d.Id] = d;
			}
		}
	}
}
", r.Sources.Find(static x => x.HintName=="N AData.cs").SourceText.ToString());
	}
	
	[Fact]
	public void SimpleIndex()
	{
		var generator = new DataTableGenerator.DataTableGenerator();
		
		/* lang=C#-test, lang=C# */
		var source = @"
namespace N
{
	[DataTableGenerator.DataTable(""Id"")]
	[DataTableGenerator.DataTableIndex(""OtherId"")]
	class AData
	{
		public int Id { get; set; }
        public int OtherId { get; set; }
	}
}
";
		
		var r = GeneratorRunner.RunAndFilter(generator.AsSourceGenerator(), "DataTableGenerator", source);
		Assert.Equal(/* lang=C#-test, lang=C# */ @"using System.Collections.Generic;
using System.Linq;

namespace N
{
	public partial class ADataStore
	{
		Dictionary<System.Int32, N.AData> UniqueIndexDictionary = new();
		public IReadOnlyDictionary<System.Int32, N.AData> UniqueIndex => UniqueIndexDictionary;
		Dictionary<System.Int32, N.AData> OtherIdDictionary = new();
		public IReadOnlyDictionary<System.Int32, N.AData> OtherIdIndex => OtherIdDictionary;
		public void SetData(IEnumerable<N.AData> data)
		{
			UniqueIndexDictionary.Clear();
			OtherIdDictionary.Clear();
			foreach (var d in data)
			{
				UniqueIndexDictionary.Add(d.Id, d);
				OtherIdDictionary.Add(d.OtherId, d);
			}
		}
		public void UpdateData(IEnumerable<N.AData> data)
		{
			foreach (var d in data)
			{
				UniqueIndexDictionary[d.Id] = d;
				OtherIdDictionary[d.OtherId] = d;
			}
		}
	}
}
", r.Sources.Find(static x => x.HintName=="N AData.cs").SourceText.ToString());
	}
	
	[Fact]
	public void ComplexIndex()
	{
		var generator = new DataTableGenerator.DataTableGenerator();
		
		/* lang=C#-test, lang=C# */
		var source = @"
namespace N
{
	[DataTableGenerator.DataTable(""Id"")]
	[DataTableGenerator.DataTableIndex(""Id"", ""Name"")]
	class AData
	{
		public int Id { get; set; }
		public string Name { get; set; }
	}
}
";
		
		var r = GeneratorRunner.RunAndFilter(generator.AsSourceGenerator(), "DataTableGenerator", source);
		Assert.Equal(/* lang=C#-test, lang=C# */ @"using System.Collections.Generic;
using System.Linq;

namespace N
{
	public partial class ADataStore
	{
		Dictionary<System.Int32, N.AData> UniqueIndexDictionary = new();
		public IReadOnlyDictionary<System.Int32, N.AData> UniqueIndex => UniqueIndexDictionary;
		Dictionary<(System.Int32 Id, System.String Name), N.AData> IdAndNameDictionary = new();
		public IReadOnlyDictionary<(System.Int32 Id, System.String Name), N.AData> IdAndNameIndex => IdAndNameDictionary;
		public void SetData(IEnumerable<N.AData> data)
		{
			UniqueIndexDictionary.Clear();
			IdAndNameDictionary.Clear();
			foreach (var d in data)
			{
				UniqueIndexDictionary.Add(d.Id, d);
				IdAndNameDictionary.Add((d.Id, d.Name), d);
			}
		}
		public void UpdateData(IEnumerable<N.AData> data)
		{
			foreach (var d in data)
			{
				UniqueIndexDictionary[d.Id] = d;
				IdAndNameDictionary[(d.Id, d.Name)] = d;
			}
		}
	}
}
", r.Sources.Find(static x => x.HintName=="N AData.cs").SourceText.ToString());
	}
	
	[Fact]
	public void MultipleIndexes()
	{
		var generator = new DataTableGenerator.DataTableGenerator();
		
		/* lang=C#-test, lang=C# */
		var source = @"
namespace N
{
	[DataTableGenerator.DataTable(""Id"")]
	[DataTableGenerator.DataTableIndex(""OtherId"")]
	[DataTableGenerator.DataTableIndex(""Id"", ""Name"")]
	class AData
	{
		public int Id { get; set; }
		public int OtherId { get; set; }
		public string Name { get; set; }
	}
}
";
		
		var r = GeneratorRunner.RunAndFilter(generator.AsSourceGenerator(), "DataTableGenerator", source);
		Assert.Equal(/* lang=C#-test, lang=C# */ @"using System.Collections.Generic;
using System.Linq;

namespace N
{
	public partial class ADataStore
	{
		Dictionary<System.Int32, N.AData> UniqueIndexDictionary = new();
		public IReadOnlyDictionary<System.Int32, N.AData> UniqueIndex => UniqueIndexDictionary;
		Dictionary<System.Int32, N.AData> OtherIdDictionary = new();
		public IReadOnlyDictionary<System.Int32, N.AData> OtherIdIndex => OtherIdDictionary;
		Dictionary<(System.Int32 Id, System.String Name), N.AData> IdAndNameDictionary = new();
		public IReadOnlyDictionary<(System.Int32 Id, System.String Name), N.AData> IdAndNameIndex => IdAndNameDictionary;
		public void SetData(IEnumerable<N.AData> data)
		{
			UniqueIndexDictionary.Clear();
			OtherIdDictionary.Clear();
			IdAndNameDictionary.Clear();
			foreach (var d in data)
			{
				UniqueIndexDictionary.Add(d.Id, d);
				OtherIdDictionary.Add(d.OtherId, d);
				IdAndNameDictionary.Add((d.Id, d.Name), d);
			}
		}
		public void UpdateData(IEnumerable<N.AData> data)
		{
			foreach (var d in data)
			{
				UniqueIndexDictionary[d.Id] = d;
				OtherIdDictionary[d.OtherId] = d;
				IdAndNameDictionary[(d.Id, d.Name)] = d;
			}
		}
	}
}
", r.Sources.Find(static x => x.HintName=="N AData.cs").SourceText.ToString());
	}
}