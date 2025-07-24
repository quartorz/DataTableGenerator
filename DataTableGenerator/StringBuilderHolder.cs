using System;
using System.Collections.Concurrent;
using System.Text;

namespace DataTableGenerator
{
	internal static class StringBuilderHolder
	{
		static readonly ConcurrentBag<StringBuilder> instances = new ConcurrentBag<StringBuilder>();

		class Disposable : IDisposable
		{
			public StringBuilder Instance;

			public void Dispose()
			{
				if (Instance != null)
				{
					Return(Instance);
					Instance = null;
				}
			}
		}

		public static IDisposable Get(out StringBuilder instance)
		{
			if (instances.TryTake(out instance))
			{
				instance.Clear();
			}
			else
			{
				instance = new StringBuilder();
			}
			return new Disposable{ Instance = instance };
		}

		public static void Return(StringBuilder instance)
		{
			instances.Add(instance);
		}
	}
}