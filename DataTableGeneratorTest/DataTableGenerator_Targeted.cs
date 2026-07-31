using Microsoft.CodeAnalysis;
using Xunit;

namespace DataTableGeneratorTest;

public class DataTableGenerator_Targeted
{
	// [GenerateDataTableStore(typeof(Row), "Key")] on a partial class generates the store body into
	// that class, reading the row type — so a row type that cannot carry [DataTable] (not partial /
	// owned elsewhere) still gets a store. Output mirrors the in-place [DataTable] form.
	[Fact]
	public void TargetedStore()
	{
		var generator = new DataTableGenerator.DataTableGenerator();

		/* lang=C#-test, lang=C# */
		var source = @"
namespace N
{
	public class UserData
	{
		public string Id { get; set; }
	}

	[DataTableGenerator.GenerateDataTableStore(typeof(UserData), ""Id"")]
	public partial class UserDataStore { }
}
";

		var r = GeneratorRunner.RunAndFilter(generator.AsSourceGenerator(), "DataTableGenerator", source);
		Assert.Equal(/* lang=C#-test, lang=C# */ @"using System.Collections.Generic;
using System.Linq;

namespace N
{
	/// <summary>
	/// Auto-generated data store for <see cref=""N.UserData""/>.
	/// </summary>
	public partial class UserDataStore
	{
		Dictionary<System.String, N.UserData> UniqueIndexDictionary = new();
		public IReadOnlyDictionary<System.String, N.UserData> UniqueIndex => UniqueIndexDictionary;
		List<N.UserData> AllDataList = new();
		public IReadOnlyList<N.UserData> AllData => AllDataList;
		partial void OnDataSet();
		partial void OnDataUpdated();
		partial void OnDataRemoved();
		public void SetData(IEnumerable<N.UserData> data)
		{
			UniqueIndexDictionary.Clear();
			foreach (var d in data)
			{
				UniqueIndexDictionary.Add(d.Id, d);
			}
			AllDataList = UniqueIndexDictionary.Values.ToList();
			OnDataSet();
		}
		public void UpdateData(IEnumerable<N.UserData> data)
		{
			foreach (var d in data)
			{
				UniqueIndexDictionary[d.Id] = d;
			}
			AllDataList = UniqueIndexDictionary.Values.ToList();
			OnDataUpdated();
		}
		public void RemoveData(IEnumerable<System.String> keys)
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
", r.Sources.Find(static x => x.HintName == "N UserDataStore.cs").SourceText.ToString());
	}
}
