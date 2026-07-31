using Microsoft.CodeAnalysis;
using Xunit;

namespace DataTableGeneratorTest;

public class DataTableGenerator_Aggregate
{
	// [GenerateAggregateStore(typeof(T))] composes a store per member:
	//  - array member -> the element type's DataTable store (SetData/UpdateData delegated),
	//  - a member type without its own store -> held whole,
	//  - SetData overwrites; UpdateData applies only non-null members (value types are unconditional).
	[Fact]
	public void AggregateComposesStoresAndHolds()
	{
		var generator = new DataTableGenerator.DataTableGenerator();

		/* lang=C#-test, lang=C# */
		var source = @"
namespace N
{
	[DataTableGenerator.DataTable(""Id"")]
	class CardMasterData { public int Id { get; set; } }

	class UserData { }

	class UserAllData
	{
		public CardMasterData[] Cards { get; set; }
		public UserData User { get; set; }
		public int Version { get; set; }
	}

	[DataTableGenerator.GenerateAggregateStore(typeof(UserAllData))]
	partial class UserStore { }
}
";

		var r = GeneratorRunner.RunAndFilter(generator.AsSourceGenerator(), "DataTableGenerator", source);
		Assert.Equal(/* lang=C#-test, lang=C# */ @"using System.Collections.Generic;
using System.Linq;

namespace N
{
	public partial class UserStore
	{
		public N.CardMasterDataStore Cards { get; } = new();
		public N.UserData User { get; private set; }
		public System.Int32 Version { get; private set; }
		public void SetData(N.UserAllData data)
		{
			Cards.SetData(data.Cards);
			User = data.User;
			Version = data.Version;
		}
		public void UpdateData(N.UserAllData data)
		{
			if (data.Cards != null) Cards.UpdateData(data.Cards);
			if (data.User != null) User = data.User;
			Version = data.Version;
		}
	}
}
", r.Sources.Find(static x => x.HintName == "N UserStore.Aggregate.cs").SourceText.ToString());
	}
}
