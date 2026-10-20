using Microsoft.CodeAnalysis;
using Xunit;

namespace DataTableGeneratorTest;

public class DataTableGenerator_Aggregate
{
	// [GenerateAggregateStore(typeof(T))] composes a store per member:
	//  - array member -> the element type's DataTable store (SetData/UpdateData delegated),
	//  - a member type without its own store -> held whole,
	//  - SetData overwrites (a delegated array must not be null; a held member may be),
	//    UpdateData applies only non-null members (value types are unconditional).
	[Fact]
	public void AggregateComposesStoresAndHolds()
	{
		var generator = new DataTableGenerator.DataTableGenerator();

		/* lang=C#-test, lang=C# */
		var source = @"
namespace N
{
	[DataTableGenerator.DataTable(""Id"")]
	public class CardMasterData { public int Id { get; set; } }

	public class UserData { }

	public class UserAllData
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
		Assert.Equal(/* lang=C#-test, lang=C# */ @"#nullable enable

using System.Collections.Generic;
using System.Linq;

namespace N
{
	public partial class UserStore
	{
		public N.CardMasterDataStore Cards { get; } = new();
		public N.UserData User { get; private set; } = null!;
		public System.Int32 Version { get; private set; }
		public void SetData(N.UserAllData data)
		{
			Cards.SetData(data.Cards ?? throw new global::System.ArgumentNullException(nameof(data.Cards)));
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

		Assert.Empty(GeneratorRunner.CompileNullableAndCollectGeneratedDiagnostics(generator.AsSourceGenerator(), source));
	}

	// An annotated element type (`Row?[]`) resolves to the store registered for `Row`; the generated
	// SetData / UpdateData reject null elements.
	[Fact]
	public void NullableElementArrayResolvesStoreAndRejectsNullElements()
	{
		var generator = new DataTableGenerator.DataTableGenerator();

		/* lang=C#-test, lang=C# */
		var source = @"
#nullable enable
namespace N
{
	[DataTableGenerator.DataTable(""Id"")]
	public class CardMasterData { public int Id { get; set; } }

	public class UserAllData
	{
		public CardMasterData?[] Cards { get; set; }
	}

	[DataTableGenerator.GenerateAggregateStore(typeof(UserAllData))]
	partial class UserStore { }
}
";

		var r = GeneratorRunner.RunAndFilter(generator.AsSourceGenerator(), "DataTableGenerator", source);
		Assert.Equal(/* lang=C#-test, lang=C# */ @"#nullable enable

using System.Collections.Generic;
using System.Linq;

namespace N
{
	public partial class UserStore
	{
		public N.CardMasterDataStore Cards { get; } = new();
		public void SetData(N.UserAllData data)
		{
			Cards.SetData((data.Cards ?? throw new global::System.ArgumentNullException(nameof(data.Cards))).Select(x => x ?? throw new global::System.ArgumentNullException(nameof(data.Cards))));
		}
		public void UpdateData(N.UserAllData data)
		{
			if (data.Cards != null) Cards.UpdateData(data.Cards.Select(x => x ?? throw new global::System.ArgumentNullException(nameof(data.Cards))));
		}
	}
}
", r.Sources.Find(static x => x.HintName == "N UserStore.Aggregate.cs").SourceText.ToString());

		Assert.Empty(GeneratorRunner.CompileNullableAndCollectGeneratedDiagnostics(generator.AsSourceGenerator(), source));
	}

	// Held-whole members keep the record's nullability (`T?` stays `T?`, a non-nullable reference type
	// gets `= null!` until SetData), nullable value types keep Nullable<T>, and the store lookup for a
	// `T[]?` member resolves to the store registered for `T`.
	[Fact]
	public void NullableAnnotatedMembersKeepNullability()
	{
		var generator = new DataTableGenerator.DataTableGenerator();

		/* lang=C#-test, lang=C# */
		var source = @"
#nullable enable
namespace N
{
	[DataTableGenerator.DataTable(""Id"")]
	public class CardMasterData { public int Id { get; set; } }

	public class UserData { }

	public class UserAllData
	{
		public UserData? User { get; set; }
		public CardMasterData[]? Cards { get; set; }
		public int? Version { get; set; }
		public int Count { get; set; }
	}

	[DataTableGenerator.GenerateAggregateStore(typeof(UserAllData))]
	partial class UserStore { }
}
";

		var r = GeneratorRunner.RunAndFilter(generator.AsSourceGenerator(), "DataTableGenerator", source);
		Assert.Equal(/* lang=C#-test, lang=C# */ @"#nullable enable

using System.Collections.Generic;
using System.Linq;

namespace N
{
	public partial class UserStore
	{
		public N.UserData? User { get; private set; }
		public N.CardMasterDataStore Cards { get; } = new();
		public System.Nullable<System.Int32> Version { get; private set; }
		public System.Int32 Count { get; private set; }
		public void SetData(N.UserAllData data)
		{
			User = data.User;
			Cards.SetData(data.Cards ?? throw new global::System.ArgumentNullException(nameof(data.Cards)));
			Version = data.Version;
			Count = data.Count;
		}
		public void UpdateData(N.UserAllData data)
		{
			if (data.User != null) User = data.User;
			if (data.Cards != null) Cards.UpdateData(data.Cards);
			if (data.Version != null) Version = data.Version;
			Count = data.Count;
		}
	}
}
", r.Sources.Find(static x => x.HintName == "N UserStore.Aggregate.cs").SourceText.ToString());

		Assert.Empty(GeneratorRunner.CompileNullableAndCollectGeneratedDiagnostics(generator.AsSourceGenerator(), source));
	}
}
