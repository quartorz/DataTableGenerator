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
	public class AData
	{
		public int Id { get; set; }
	}
}
";
		
		var r = GeneratorRunner.RunAndFilter(generator.AsSourceGenerator(), "DataTableGenerator", source1);
		Assert.Empty(GeneratorRunner.CompileNullableAndCollectGeneratedDiagnostics(generator.AsSourceGenerator(), source1));
		Assert.Equal(/* lang=C#-test, lang=C# */ @"#nullable enable

using System.Collections.Generic;
using System.Linq;

namespace N
{
	/// <summary>
	/// Auto-generated data store for <see cref=""N.AData""/>.
	/// </summary>
	public partial class ADataStore
	{
		Dictionary<System.Int32, N.AData> UniqueIndexDictionary = new();
		public IReadOnlyDictionary<System.Int32, N.AData> UniqueIndex => UniqueIndexDictionary;
		List<N.AData> AllDataList = new();
		public IReadOnlyList<N.AData> AllData => AllDataList;
		partial void OnDataSet();
		partial void OnDataUpdated();
		partial void OnDataRemoved();
		public void SetData(IEnumerable<N.AData> data)
		{
			UniqueIndexDictionary.Clear();
			foreach (var d in data)
			{
				UniqueIndexDictionary.Add(d.Id, d);
			}
			AllDataList = UniqueIndexDictionary.Values.ToList();
			OnDataSet();
		}
		public void UpdateData(IEnumerable<N.AData> data)
		{
			foreach (var d in data)
			{
				UniqueIndexDictionary[d.Id] = d;
			}
			AllDataList = UniqueIndexDictionary.Values.ToList();
			OnDataUpdated();
		}
		public void RemoveData(IEnumerable<System.Int32> keys)
		{
			foreach (var key in keys)
			{
				UniqueIndexDictionary.Remove(key);
			}
			AllDataList = UniqueIndexDictionary.Values.ToList();
			OnDataRemoved();
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
	public class AData
	{
		public int Id { get; set; }
        public int OtherId { get; set; }
	}
}
";
		
		var r = GeneratorRunner.RunAndFilter(generator.AsSourceGenerator(), "DataTableGenerator", source);
		Assert.Empty(GeneratorRunner.CompileNullableAndCollectGeneratedDiagnostics(generator.AsSourceGenerator(), source));
		Assert.Equal(/* lang=C#-test, lang=C# */ @"#nullable enable

using System.Collections.Generic;
using System.Linq;

namespace N
{
	/// <summary>
	/// Auto-generated data store for <see cref=""N.AData""/>.
	/// </summary>
	public partial class ADataStore
	{
		Dictionary<System.Int32, N.AData> UniqueIndexDictionary = new();
		public IReadOnlyDictionary<System.Int32, N.AData> UniqueIndex => UniqueIndexDictionary;
		List<N.AData> AllDataList = new();
		public IReadOnlyList<N.AData> AllData => AllDataList;
		Dictionary<System.Int32, N.AData> OtherIdDictionary = new();
		public IReadOnlyDictionary<System.Int32, N.AData> OtherIdIndex => OtherIdDictionary;
		partial void OnDataSet();
		partial void OnDataUpdated();
		partial void OnDataRemoved();
		public void SetData(IEnumerable<N.AData> data)
		{
			UniqueIndexDictionary.Clear();
			OtherIdDictionary.Clear();
			foreach (var d in data)
			{
				UniqueIndexDictionary.Add(d.Id, d);
				OtherIdDictionary.Add(d.OtherId, d);
			}
			AllDataList = UniqueIndexDictionary.Values.ToList();
			OnDataSet();
		}
		public void UpdateData(IEnumerable<N.AData> data)
		{
			foreach (var d in data)
			{
				UniqueIndexDictionary[d.Id] = d;
			}
			OtherIdDictionary.Clear();
			foreach (var d in UniqueIndexDictionary.Values)
			{
				OtherIdDictionary[d.OtherId] = d;
			}
			AllDataList = UniqueIndexDictionary.Values.ToList();
			OnDataUpdated();
		}
		public void RemoveData(IEnumerable<System.Int32> keys)
		{
			foreach (var key in keys)
			{
				UniqueIndexDictionary.Remove(key);
			}
			OtherIdDictionary.Clear();
			foreach (var d in UniqueIndexDictionary.Values)
			{
				OtherIdDictionary[d.OtherId] = d;
			}
			AllDataList = UniqueIndexDictionary.Values.ToList();
			OnDataRemoved();
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
	public class AData
	{
		public int Id { get; set; }
		public string Name { get; set; }
	}
}
";
		
		var r = GeneratorRunner.RunAndFilter(generator.AsSourceGenerator(), "DataTableGenerator", source);
		Assert.Empty(GeneratorRunner.CompileNullableAndCollectGeneratedDiagnostics(generator.AsSourceGenerator(), source));
		Assert.Equal(/* lang=C#-test, lang=C# */ @"#nullable enable

using System.Collections.Generic;
using System.Linq;

namespace N
{
	/// <summary>
	/// Auto-generated data store for <see cref=""N.AData""/>.
	/// </summary>
	public partial class ADataStore
	{
		Dictionary<System.Int32, N.AData> UniqueIndexDictionary = new();
		public IReadOnlyDictionary<System.Int32, N.AData> UniqueIndex => UniqueIndexDictionary;
		List<N.AData> AllDataList = new();
		public IReadOnlyList<N.AData> AllData => AllDataList;
		Dictionary<(System.Int32 Id, System.String Name), N.AData> IdAndNameDictionary = new();
		public IReadOnlyDictionary<(System.Int32 Id, System.String Name), N.AData> IdAndNameIndex => IdAndNameDictionary;
		partial void OnDataSet();
		partial void OnDataUpdated();
		partial void OnDataRemoved();
		public void SetData(IEnumerable<N.AData> data)
		{
			UniqueIndexDictionary.Clear();
			IdAndNameDictionary.Clear();
			foreach (var d in data)
			{
				UniqueIndexDictionary.Add(d.Id, d);
				IdAndNameDictionary.Add((d.Id, d.Name), d);
			}
			AllDataList = UniqueIndexDictionary.Values.ToList();
			OnDataSet();
		}
		public void UpdateData(IEnumerable<N.AData> data)
		{
			foreach (var d in data)
			{
				UniqueIndexDictionary[d.Id] = d;
			}
			IdAndNameDictionary.Clear();
			foreach (var d in UniqueIndexDictionary.Values)
			{
				IdAndNameDictionary[(d.Id, d.Name)] = d;
			}
			AllDataList = UniqueIndexDictionary.Values.ToList();
			OnDataUpdated();
		}
		public void RemoveData(IEnumerable<System.Int32> keys)
		{
			foreach (var key in keys)
			{
				UniqueIndexDictionary.Remove(key);
			}
			IdAndNameDictionary.Clear();
			foreach (var d in UniqueIndexDictionary.Values)
			{
				IdAndNameDictionary[(d.Id, d.Name)] = d;
			}
			AllDataList = UniqueIndexDictionary.Values.ToList();
			OnDataRemoved();
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
	public class AData
	{
		public int Id { get; set; }
		public string Name { get; set; }
	}
}
";

		var r = GeneratorRunner.RunAndFilter(generator.AsSourceGenerator(), "DataTableGenerator", source);
		Assert.Empty(GeneratorRunner.CompileNullableAndCollectGeneratedDiagnostics(generator.AsSourceGenerator(), source));
		Assert.Equal(/* lang=C#-test, lang=C# */ @"#nullable enable

using System.Collections.Generic;
using System.Linq;

namespace N
{
	/// <summary>
	/// Auto-generated data store for <see cref=""N.AData""/>.
	/// </summary>
	public partial class ADataStore
	{
		Dictionary<System.Int32, N.AData> UniqueIndexDictionary = new();
		public IReadOnlyDictionary<System.Int32, N.AData> UniqueIndex => UniqueIndexDictionary;
		List<N.AData> AllDataList = new();
		public IReadOnlyList<N.AData> AllData => AllDataList;
		List<N.AData> SortedByNameList = new();
		public IReadOnlyList<N.AData> SortedByName => SortedByNameList;
		partial void OnDataSet();
		partial void OnDataUpdated();
		partial void OnDataRemoved();
		public void SetData(IEnumerable<N.AData> data)
		{
			var dataList = data.ToList();
			UniqueIndexDictionary.Clear();
			foreach (var d in dataList)
			{
				UniqueIndexDictionary.Add(d.Id, d);
			}
			SortedByNameList = dataList.OrderBy(d => d.Name).ToList();
			AllDataList = UniqueIndexDictionary.Values.ToList();
			OnDataSet();
		}
		public void UpdateData(IEnumerable<N.AData> data)
		{
			foreach (var d in data)
			{
				UniqueIndexDictionary[d.Id] = d;
			}
			SortedByNameList = UniqueIndexDictionary.Values.OrderBy(d => d.Name).ToList();
			AllDataList = UniqueIndexDictionary.Values.ToList();
			OnDataUpdated();
		}
		public void RemoveData(IEnumerable<System.Int32> keys)
		{
			foreach (var key in keys)
			{
				UniqueIndexDictionary.Remove(key);
			}
			SortedByNameList = UniqueIndexDictionary.Values.OrderBy(d => d.Name).ToList();
			AllDataList = UniqueIndexDictionary.Values.ToList();
			OnDataRemoved();
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
	public class AData
	{
		public int Id { get; set; }
		public string Name { get; set; }
	}
}
";

		var r = GeneratorRunner.RunAndFilter(generator.AsSourceGenerator(), "DataTableGenerator", source);
		Assert.Empty(GeneratorRunner.CompileNullableAndCollectGeneratedDiagnostics(generator.AsSourceGenerator(), source));
		Assert.Equal(/* lang=C#-test, lang=C# */ @"#nullable enable

using System.Collections.Generic;
using System.Linq;

namespace N
{
	/// <summary>
	/// Auto-generated data store for <see cref=""N.AData""/>.
	/// </summary>
	public partial class ADataStore
	{
		Dictionary<System.Int32, N.AData> UniqueIndexDictionary = new();
		public IReadOnlyDictionary<System.Int32, N.AData> UniqueIndex => UniqueIndexDictionary;
		List<N.AData> AllDataList = new();
		public IReadOnlyList<N.AData> AllData => AllDataList;
		List<N.AData> SortedByNameDescList = new();
		public IReadOnlyList<N.AData> SortedByNameDesc => SortedByNameDescList;
		partial void OnDataSet();
		partial void OnDataUpdated();
		partial void OnDataRemoved();
		public void SetData(IEnumerable<N.AData> data)
		{
			var dataList = data.ToList();
			UniqueIndexDictionary.Clear();
			foreach (var d in dataList)
			{
				UniqueIndexDictionary.Add(d.Id, d);
			}
			SortedByNameDescList = dataList.OrderByDescending(d => d.Name).ToList();
			AllDataList = UniqueIndexDictionary.Values.ToList();
			OnDataSet();
		}
		public void UpdateData(IEnumerable<N.AData> data)
		{
			foreach (var d in data)
			{
				UniqueIndexDictionary[d.Id] = d;
			}
			SortedByNameDescList = UniqueIndexDictionary.Values.OrderByDescending(d => d.Name).ToList();
			AllDataList = UniqueIndexDictionary.Values.ToList();
			OnDataUpdated();
		}
		public void RemoveData(IEnumerable<System.Int32> keys)
		{
			foreach (var key in keys)
			{
				UniqueIndexDictionary.Remove(key);
			}
			SortedByNameDescList = UniqueIndexDictionary.Values.OrderByDescending(d => d.Name).ToList();
			AllDataList = UniqueIndexDictionary.Values.ToList();
			OnDataRemoved();
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
	public class AData
	{
		public int Id { get; set; }
		public int Level { get; set; }
		public string Name { get; set; }
	}
}
";

		var r = GeneratorRunner.RunAndFilter(generator.AsSourceGenerator(), "DataTableGenerator", source);
		Assert.Empty(GeneratorRunner.CompileNullableAndCollectGeneratedDiagnostics(generator.AsSourceGenerator(), source));
		Assert.Equal(/* lang=C#-test, lang=C# */ @"#nullable enable

using System.Collections.Generic;
using System.Linq;

namespace N
{
	/// <summary>
	/// Auto-generated data store for <see cref=""N.AData""/>.
	/// </summary>
	public partial class ADataStore
	{
		Dictionary<System.Int32, N.AData> UniqueIndexDictionary = new();
		public IReadOnlyDictionary<System.Int32, N.AData> UniqueIndex => UniqueIndexDictionary;
		List<N.AData> AllDataList = new();
		public IReadOnlyList<N.AData> AllData => AllDataList;
		List<N.AData> SortedByLevelDescAndNameList = new();
		public IReadOnlyList<N.AData> SortedByLevelDescAndName => SortedByLevelDescAndNameList;
		partial void OnDataSet();
		partial void OnDataUpdated();
		partial void OnDataRemoved();
		public void SetData(IEnumerable<N.AData> data)
		{
			var dataList = data.ToList();
			UniqueIndexDictionary.Clear();
			foreach (var d in dataList)
			{
				UniqueIndexDictionary.Add(d.Id, d);
			}
			SortedByLevelDescAndNameList = dataList.OrderByDescending(d => d.Level).ThenBy(d => d.Name).ToList();
			AllDataList = UniqueIndexDictionary.Values.ToList();
			OnDataSet();
		}
		public void UpdateData(IEnumerable<N.AData> data)
		{
			foreach (var d in data)
			{
				UniqueIndexDictionary[d.Id] = d;
			}
			SortedByLevelDescAndNameList = UniqueIndexDictionary.Values.OrderByDescending(d => d.Level).ThenBy(d => d.Name).ToList();
			AllDataList = UniqueIndexDictionary.Values.ToList();
			OnDataUpdated();
		}
		public void RemoveData(IEnumerable<System.Int32> keys)
		{
			foreach (var key in keys)
			{
				UniqueIndexDictionary.Remove(key);
			}
			SortedByLevelDescAndNameList = UniqueIndexDictionary.Values.OrderByDescending(d => d.Level).ThenBy(d => d.Name).ToList();
			AllDataList = UniqueIndexDictionary.Values.ToList();
			OnDataRemoved();
		}
	}
}
", r.Sources.Find(static x => x.HintName=="N AData.cs").SourceText.ToString());
	}

	[Fact]
	public void SortWithComparer()
	{
		var generator = new DataTableGenerator.DataTableGenerator();

		/* lang=C#-test, lang=C# */
		var source = @"
using System.Collections.Generic;
namespace N
{
	[DataTableGenerator.DataTable(""Id"")]
	[DataTableGenerator.DataTableSort(""Name"")]
	[DataTableGenerator.DataTableSortComparer(""Name"", typeof(NameComparer))]
	public class AData
	{
		public int Id { get; set; }
		public string Name { get; set; }
	}
	public class NameComparer : IComparer<string>
	{
		public int Compare(string x, string y) => 0;
	}
}
";

		var r = GeneratorRunner.RunAndFilter(generator.AsSourceGenerator(), "DataTableGenerator", source);
		Assert.Empty(GeneratorRunner.CompileNullableAndCollectGeneratedDiagnostics(generator.AsSourceGenerator(), source));
		Assert.Equal(/* lang=C#-test, lang=C# */ @"#nullable enable

using System.Collections.Generic;
using System.Linq;

namespace N
{
	/// <summary>
	/// Auto-generated data store for <see cref=""N.AData""/>.
	/// </summary>
	public partial class ADataStore
	{
		Dictionary<System.Int32, N.AData> UniqueIndexDictionary = new();
		public IReadOnlyDictionary<System.Int32, N.AData> UniqueIndex => UniqueIndexDictionary;
		List<N.AData> AllDataList = new();
		public IReadOnlyList<N.AData> AllData => AllDataList;
		List<N.AData> SortedByNameList = new();
		public IReadOnlyList<N.AData> SortedByName => SortedByNameList;
		partial void OnDataSet();
		partial void OnDataUpdated();
		partial void OnDataRemoved();
		public void SetData(IEnumerable<N.AData> data)
		{
			var dataList = data.ToList();
			UniqueIndexDictionary.Clear();
			foreach (var d in dataList)
			{
				UniqueIndexDictionary.Add(d.Id, d);
			}
			SortedByNameList = dataList.OrderBy(d => d.Name, new N.NameComparer()).ToList();
			AllDataList = UniqueIndexDictionary.Values.ToList();
			OnDataSet();
		}
		public void UpdateData(IEnumerable<N.AData> data)
		{
			foreach (var d in data)
			{
				UniqueIndexDictionary[d.Id] = d;
			}
			SortedByNameList = UniqueIndexDictionary.Values.OrderBy(d => d.Name, new N.NameComparer()).ToList();
			AllDataList = UniqueIndexDictionary.Values.ToList();
			OnDataUpdated();
		}
		public void RemoveData(IEnumerable<System.Int32> keys)
		{
			foreach (var key in keys)
			{
				UniqueIndexDictionary.Remove(key);
			}
			SortedByNameList = UniqueIndexDictionary.Values.OrderBy(d => d.Name, new N.NameComparer()).ToList();
			AllDataList = UniqueIndexDictionary.Values.ToList();
			OnDataRemoved();
		}
	}
}
", r.Sources.Find(static x => x.HintName=="N AData.cs").SourceText.ToString());
	}

	[Fact]
	public void MultipleKeysSortWithComparer()
	{
		var generator = new DataTableGenerator.DataTableGenerator();

		/* lang=C#-test, lang=C# */
		var source = @"
using System.Collections.Generic;
namespace N
{
	[DataTableGenerator.DataTable(""Id"")]
	[DataTableGenerator.DataTableSort(""Level:desc"", ""Name"")]
	[DataTableGenerator.DataTableSortComparer(""Name"", typeof(NameComparer))]
	public class AData
	{
		public int Id { get; set; }
		public int Level { get; set; }
		public string Name { get; set; }
	}
	public class NameComparer : IComparer<string>
	{
		public int Compare(string x, string y) => 0;
	}
}
";

		var r = GeneratorRunner.RunAndFilter(generator.AsSourceGenerator(), "DataTableGenerator", source);
		Assert.Empty(GeneratorRunner.CompileNullableAndCollectGeneratedDiagnostics(generator.AsSourceGenerator(), source));
		Assert.Equal(/* lang=C#-test, lang=C# */ @"#nullable enable

using System.Collections.Generic;
using System.Linq;

namespace N
{
	/// <summary>
	/// Auto-generated data store for <see cref=""N.AData""/>.
	/// </summary>
	public partial class ADataStore
	{
		Dictionary<System.Int32, N.AData> UniqueIndexDictionary = new();
		public IReadOnlyDictionary<System.Int32, N.AData> UniqueIndex => UniqueIndexDictionary;
		List<N.AData> AllDataList = new();
		public IReadOnlyList<N.AData> AllData => AllDataList;
		List<N.AData> SortedByLevelDescAndNameList = new();
		public IReadOnlyList<N.AData> SortedByLevelDescAndName => SortedByLevelDescAndNameList;
		partial void OnDataSet();
		partial void OnDataUpdated();
		partial void OnDataRemoved();
		public void SetData(IEnumerable<N.AData> data)
		{
			var dataList = data.ToList();
			UniqueIndexDictionary.Clear();
			foreach (var d in dataList)
			{
				UniqueIndexDictionary.Add(d.Id, d);
			}
			SortedByLevelDescAndNameList = dataList.OrderByDescending(d => d.Level).ThenBy(d => d.Name, new N.NameComparer()).ToList();
			AllDataList = UniqueIndexDictionary.Values.ToList();
			OnDataSet();
		}
		public void UpdateData(IEnumerable<N.AData> data)
		{
			foreach (var d in data)
			{
				UniqueIndexDictionary[d.Id] = d;
			}
			SortedByLevelDescAndNameList = UniqueIndexDictionary.Values.OrderByDescending(d => d.Level).ThenBy(d => d.Name, new N.NameComparer()).ToList();
			AllDataList = UniqueIndexDictionary.Values.ToList();
			OnDataUpdated();
		}
		public void RemoveData(IEnumerable<System.Int32> keys)
		{
			foreach (var key in keys)
			{
				UniqueIndexDictionary.Remove(key);
			}
			SortedByLevelDescAndNameList = UniqueIndexDictionary.Values.OrderByDescending(d => d.Level).ThenBy(d => d.Name, new N.NameComparer()).ToList();
			AllDataList = UniqueIndexDictionary.Values.ToList();
			OnDataRemoved();
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
	public class AData
	{
		public int Id { get; set; }
		public int OtherId { get; set; }
		public string Name { get; set; }
	}
}
";
		
		var r = GeneratorRunner.RunAndFilter(generator.AsSourceGenerator(), "DataTableGenerator", source);
		Assert.Empty(GeneratorRunner.CompileNullableAndCollectGeneratedDiagnostics(generator.AsSourceGenerator(), source));
		Assert.Equal(/* lang=C#-test, lang=C# */ @"#nullable enable

using System.Collections.Generic;
using System.Linq;

namespace N
{
	/// <summary>
	/// Auto-generated data store for <see cref=""N.AData""/>.
	/// </summary>
	public partial class ADataStore
	{
		Dictionary<System.Int32, N.AData> UniqueIndexDictionary = new();
		public IReadOnlyDictionary<System.Int32, N.AData> UniqueIndex => UniqueIndexDictionary;
		List<N.AData> AllDataList = new();
		public IReadOnlyList<N.AData> AllData => AllDataList;
		Dictionary<System.Int32, N.AData> OtherIdDictionary = new();
		public IReadOnlyDictionary<System.Int32, N.AData> OtherIdIndex => OtherIdDictionary;
		Dictionary<(System.Int32 Id, System.String Name), N.AData> IdAndNameDictionary = new();
		public IReadOnlyDictionary<(System.Int32 Id, System.String Name), N.AData> IdAndNameIndex => IdAndNameDictionary;
		partial void OnDataSet();
		partial void OnDataUpdated();
		partial void OnDataRemoved();
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
			AllDataList = UniqueIndexDictionary.Values.ToList();
			OnDataSet();
		}
		public void UpdateData(IEnumerable<N.AData> data)
		{
			foreach (var d in data)
			{
				UniqueIndexDictionary[d.Id] = d;
			}
			OtherIdDictionary.Clear();
			IdAndNameDictionary.Clear();
			foreach (var d in UniqueIndexDictionary.Values)
			{
				OtherIdDictionary[d.OtherId] = d;
				IdAndNameDictionary[(d.Id, d.Name)] = d;
			}
			AllDataList = UniqueIndexDictionary.Values.ToList();
			OnDataUpdated();
		}
		public void RemoveData(IEnumerable<System.Int32> keys)
		{
			foreach (var key in keys)
			{
				UniqueIndexDictionary.Remove(key);
			}
			OtherIdDictionary.Clear();
			IdAndNameDictionary.Clear();
			foreach (var d in UniqueIndexDictionary.Values)
			{
				OtherIdDictionary[d.OtherId] = d;
				IdAndNameDictionary[(d.Id, d.Name)] = d;
			}
			AllDataList = UniqueIndexDictionary.Values.ToList();
			OnDataRemoved();
		}
	}
}
", r.Sources.Find(static x => x.HintName=="N AData.cs").SourceText.ToString());
	}
}