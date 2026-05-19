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
		partial void OnDataSet();
		partial void OnDataUpdated();
		public void SetData(IEnumerable<N.AData> data)
		{
			UniqueIndexDictionary.Clear();
			foreach (var d in data)
			{
				UniqueIndexDictionary.Add(d.Id, d);
			}
			OnDataSet();
		}
		public void UpdateData(IEnumerable<N.AData> data)
		{
			foreach (var d in data)
			{
				UniqueIndexDictionary[d.Id] = d;
			}
			OnDataUpdated();
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
		partial void OnDataSet();
		partial void OnDataUpdated();
		public void SetData(IEnumerable<N.AData> data)
		{
			UniqueIndexDictionary.Clear();
			OtherIdDictionary.Clear();
			foreach (var d in data)
			{
				UniqueIndexDictionary.Add(d.Id, d);
				OtherIdDictionary.Add(d.OtherId, d);
			}
			OnDataSet();
		}
		public void UpdateData(IEnumerable<N.AData> data)
		{
			foreach (var d in data)
			{
				UniqueIndexDictionary[d.Id] = d;
				OtherIdDictionary[d.OtherId] = d;
			}
			OnDataUpdated();
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
		partial void OnDataSet();
		partial void OnDataUpdated();
		public void SetData(IEnumerable<N.AData> data)
		{
			UniqueIndexDictionary.Clear();
			IdAndNameDictionary.Clear();
			foreach (var d in data)
			{
				UniqueIndexDictionary.Add(d.Id, d);
				IdAndNameDictionary.Add((d.Id, d.Name), d);
			}
			OnDataSet();
		}
		public void UpdateData(IEnumerable<N.AData> data)
		{
			foreach (var d in data)
			{
				UniqueIndexDictionary[d.Id] = d;
				IdAndNameDictionary[(d.Id, d.Name)] = d;
			}
			OnDataUpdated();
		}
	}
}
", r.Sources.Find(static x => x.HintName=="N AData.cs").SourceText.ToString());
	}
	
	[Fact]
	public void SimpleSort()
	{
		var generator = new DataTableGenerator.DataTableGenerator();

		/* lang=C#-test, lang=C# */
		var source = @"
namespace N
{
	[DataTableGenerator.DataTable(""Id"")]
	[DataTableGenerator.DataTableSort(""Name"")]
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
		List<N.AData> SortedByNameList = new();
		public IReadOnlyList<N.AData> SortedByName => SortedByNameList;
		partial void OnDataSet();
		partial void OnDataUpdated();
		public void SetData(IEnumerable<N.AData> data)
		{
			var dataList = data.ToList();
			UniqueIndexDictionary.Clear();
			foreach (var d in dataList)
			{
				UniqueIndexDictionary.Add(d.Id, d);
			}
			SortedByNameList = dataList.OrderBy(d => d.Name).ToList();
			OnDataSet();
		}
		public void UpdateData(IEnumerable<N.AData> data)
		{
			foreach (var d in data)
			{
				UniqueIndexDictionary[d.Id] = d;
			}
			OnDataUpdated();
		}
	}
}
", r.Sources.Find(static x => x.HintName=="N AData.cs").SourceText.ToString());
	}

	[Fact]
	public void SimpleSortDesc()
	{
		var generator = new DataTableGenerator.DataTableGenerator();

		/* lang=C#-test, lang=C# */
		var source = @"
namespace N
{
	[DataTableGenerator.DataTable(""Id"")]
	[DataTableGenerator.DataTableSort(""Name:desc"")]
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
		List<N.AData> SortedByNameDescList = new();
		public IReadOnlyList<N.AData> SortedByNameDesc => SortedByNameDescList;
		partial void OnDataSet();
		partial void OnDataUpdated();
		public void SetData(IEnumerable<N.AData> data)
		{
			var dataList = data.ToList();
			UniqueIndexDictionary.Clear();
			foreach (var d in dataList)
			{
				UniqueIndexDictionary.Add(d.Id, d);
			}
			SortedByNameDescList = dataList.OrderByDescending(d => d.Name).ToList();
			OnDataSet();
		}
		public void UpdateData(IEnumerable<N.AData> data)
		{
			foreach (var d in data)
			{
				UniqueIndexDictionary[d.Id] = d;
			}
			OnDataUpdated();
		}
	}
}
", r.Sources.Find(static x => x.HintName=="N AData.cs").SourceText.ToString());
	}

	[Fact]
	public void MultipleKeysSort()
	{
		var generator = new DataTableGenerator.DataTableGenerator();

		/* lang=C#-test, lang=C# */
		var source = @"
namespace N
{
	[DataTableGenerator.DataTable(""Id"")]
	[DataTableGenerator.DataTableSort(""Level:desc"", ""Name"")]
	class AData
	{
		public int Id { get; set; }
		public int Level { get; set; }
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
		List<N.AData> SortedByLevelDescAndNameList = new();
		public IReadOnlyList<N.AData> SortedByLevelDescAndName => SortedByLevelDescAndNameList;
		partial void OnDataSet();
		partial void OnDataUpdated();
		public void SetData(IEnumerable<N.AData> data)
		{
			var dataList = data.ToList();
			UniqueIndexDictionary.Clear();
			foreach (var d in dataList)
			{
				UniqueIndexDictionary.Add(d.Id, d);
			}
			SortedByLevelDescAndNameList = dataList.OrderByDescending(d => d.Level).ThenBy(d => d.Name).ToList();
			OnDataSet();
		}
		public void UpdateData(IEnumerable<N.AData> data)
		{
			foreach (var d in data)
			{
				UniqueIndexDictionary[d.Id] = d;
			}
			OnDataUpdated();
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
		partial void OnDataSet();
		partial void OnDataUpdated();
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
			OnDataSet();
		}
		public void UpdateData(IEnumerable<N.AData> data)
		{
			foreach (var d in data)
			{
				UniqueIndexDictionary[d.Id] = d;
				OtherIdDictionary[d.OtherId] = d;
				IdAndNameDictionary[(d.Id, d.Name)] = d;
			}
			OnDataUpdated();
		}
	}
}
", r.Sources.Find(static x => x.HintName=="N AData.cs").SourceText.ToString());
	}
}