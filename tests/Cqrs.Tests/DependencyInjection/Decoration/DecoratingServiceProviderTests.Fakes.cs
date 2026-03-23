namespace KatzuoOgust.Cqrs.DependencyInjection.Decoration;

public sealed partial class DecoratingServiceProviderTests
{
	private interface IFoo
	{
		public string Name { get; }
	}

	private interface IBar<T>
	{
		public T Value { get; }
	}

	private interface IDep
	{
		public string Value { get; }
	}

	private sealed class Foo(string name) : IFoo
	{
		public string Name => name;
	}

	private sealed class WrappedFoo(IFoo inner) : IFoo
	{
		public string Name => $"wrapped({inner.Name})";
	}

	private sealed class SpCapturingFoo(IFoo inner, IServiceProvider sp) : IFoo
	{
		public string Name => inner.Name;
		public IServiceProvider CapturedSp => sp;
	}

	private sealed class FooWithDep(IFoo inner, IDep dep) : IFoo
	{
		public string Name => $"{inner.Name}+{dep.Value}";
	}

	private sealed class NotAFooDecorator(IFoo inner)
	{
		public string Name => inner.Name;
	}

	private sealed class NoServiceCtorDecorator(string x) : IFoo
	{
		public string Name => x;
	}

	private sealed class Dep(string value) : IDep
	{
		public string Value => value;
	}

	private sealed class Bar<T>(T value) : IBar<T>
	{
		public T Value => value;
	}

	private sealed class WrappedBar<T>(IBar<T> inner) : IBar<T>
	{
		public T Value => inner.Value;
	}

	private sealed class BadBarDecorator<T>(IBar<T> inner)
	{
	}
}
