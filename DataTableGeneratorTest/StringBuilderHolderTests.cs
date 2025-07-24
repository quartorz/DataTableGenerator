using DataTableGenerator;
using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace DataTableGeneratorTest;

public class StringBuilderHolderTests
{
	[Property(MaxTest = 1)]
	public void Get(NonEmptyString s1, NonEmptyString s2)
	{
		var d1 = StringBuilderHolder.Get(out var sb1);
		var d2 = StringBuilderHolder.Get(out var sb2);
		
		Assert.NotSame(sb1, sb2);

		sb1.Append(s1);
		sb2.Append(s2);
		Assert.Equal(s1.Get, sb1.ToString());
		Assert.Equal(s2.Get, sb2.ToString());
		
		d1.Dispose();
		d2.Dispose();
		
		Assert.Equal(sb1.Length, s1.Get.Length);
		Assert.Equal(sb2.Length, s2.Get.Length);
		
		using var _ = StringBuilderHolder.Get(out var sb3);
		Assert.True(ReferenceEquals(sb3, sb1) || ReferenceEquals(sb3, sb2));
		Assert.Equal(0, sb3.Length);
	}
}