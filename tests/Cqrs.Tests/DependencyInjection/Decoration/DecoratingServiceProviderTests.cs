namespace KatzuoOgust.Cqrs.DependencyInjection.Decoration;

public sealed partial class DecoratingServiceProviderTests
{
	#region Construction

	[Fact]
	public void Ctor_ThrowsArgumentNullException_WhenInnerIsNull() =>
		Assert.Throws<ArgumentNullException>(() => new DecoratingServiceProvider(null!));

	#endregion

	#region Exact decorator

	[Fact]
	public void GetService_ReturnsInnerService_WhenNoDecoratorsRegistered()
	{
		var sp = new SimpleServiceProvider();
		sp.Register<IFoo>(new Foo("a"));

		var result = new DecoratingServiceProvider(sp).GetService(typeof(IFoo));

		Assert.IsType<Foo>(result);
	}

	[Fact]
	public void GetService_WrapsResolvedService_WhenExactDecoratorRegistered()
	{
		var sp = new SimpleServiceProvider();
		sp.Register<IFoo>(new Foo("a"));

		var result = new DecoratingServiceProvider(sp)
			.Decorate<IFoo>((inner, _) => new WrappedFoo(inner))
			.GetService(typeof(IFoo));

		Assert.Equal("wrapped(a)", ((IFoo)result!).Name);
	}

	[Fact]
	public void GetService_ReturnsNull_WhenInnerReturnsNull()
	{
		var result = new DecoratingServiceProvider(new SimpleServiceProvider())
			.Decorate<IFoo>((inner, _) => new WrappedFoo(inner))
			.GetService(typeof(IFoo));

		Assert.Null(result);
	}

	[Fact]
	public void GetService_PassesSelfAsServiceProvider_WhenDecoratorRegistered()
	{
		var sp = new SimpleServiceProvider();
		sp.Register<IFoo>(new Foo("a"));
		IServiceProvider? captured = null;

		var dsp = new DecoratingServiceProvider(sp)
			.Decorate<IFoo>((inner, p) => { captured = p; return inner; });

		dsp.GetService(typeof(IFoo));

		Assert.Same(dsp, captured);
	}

	[Fact]
	public void Decorate_ThrowsArgumentNullException_WhenDecoratorIsNull() =>
		Assert.Throws<ArgumentNullException>(() =>
			new DecoratingServiceProvider(new SimpleServiceProvider())
				.Decorate<IFoo>(null!));

	#endregion

	#region Open-generic decorator

	[Fact]
	public void GetService_WrapsResolvedService_WhenGenericDecoratorRegistered()
	{
		var sp = new SimpleServiceProvider();
		sp.Register<IBar<int>>(new Bar<int>(42));

		var result = (IBar<int>?)new DecoratingServiceProvider(sp)
			.Decorate(typeof(IBar<>), (type, inner, _) =>
			{
				var wrapped = typeof(WrappedBar<>).MakeGenericType(type.GetGenericArguments());
				return Activator.CreateInstance(wrapped, inner)!;
			})
			.GetService(typeof(IBar<int>));

		Assert.Equal(42, result!.Value);
		Assert.IsType<WrappedBar<int>>(result);
	}

	[Fact]
	public void GetService_PassesThrough_WhenGenericDecoratorDoesNotMatchType()
	{
		var sp = new SimpleServiceProvider();
		sp.Register<IFoo>(new Foo("a"));

		var result = new DecoratingServiceProvider(sp)
			.Decorate(typeof(IBar<>), (_, _, _) => throw new InvalidOperationException("should not be called"))
			.GetService(typeof(IFoo));

		Assert.IsType<Foo>(result);
	}

	[Fact]
	public void Decorate_ThrowsArgumentException_WhenServiceTypeIsNotOpenGeneric() =>
		Assert.Throws<ArgumentException>(() =>
			new DecoratingServiceProvider(new SimpleServiceProvider())
				.Decorate(typeof(IBar<int>), (_, inner, _) => inner));

	[Fact]
	public void Decorate_ThrowsArgumentNullException_WhenGenericDecoratorIsNull() =>
		Assert.Throws<ArgumentNullException>(() =>
			new DecoratingServiceProvider(new SimpleServiceProvider())
				.Decorate(typeof(IBar<>), (Func<Type, object, IServiceProvider, object>)null!));

	[Fact]
	public void Decorate_ThrowsArgumentNullException_WhenOpenGenericTypeIsNull() =>
		Assert.Throws<ArgumentNullException>(() =>
			new DecoratingServiceProvider(new SimpleServiceProvider())
				.Decorate(null!, (_, inner, _) => inner));

	#endregion

	#region Multiple decorators

	[Fact]
	public void GetService_AppliesDecoratorsInnermostFirst_WhenMultipleExactDecoratorsRegistered()
	{
		var sp = new SimpleServiceProvider();
		sp.Register<IFoo>(new Foo("x"));

		var result = (IFoo?)new DecoratingServiceProvider(sp)
			.Decorate<IFoo>((inner, _) => new WrappedFoo(inner))   // 1st → wrapped(x)
			.Decorate<IFoo>((inner, _) => new WrappedFoo(inner))   // 2nd → wrapped(wrapped(x))
			.GetService(typeof(IFoo));

		Assert.Equal("wrapped(wrapped(x))", result!.Name);
	}

	[Fact]
	public void GetService_AppliesDecoratorsInnermostFirst_WhenMultipleGenericDecoratorsRegistered()
	{
		var sp = new SimpleServiceProvider();
		sp.Register<IBar<int>>(new Bar<int>(7));

		Func<Type, object, IServiceProvider, object> wrap = (type, inner, _) =>
		{
			var wrapped = typeof(WrappedBar<>).MakeGenericType(type.GetGenericArguments());
			return Activator.CreateInstance(wrapped, inner)!;
		};

		var result = (IBar<int>?)new DecoratingServiceProvider(sp)
			.Decorate(typeof(IBar<>), wrap)
			.Decorate(typeof(IBar<>), wrap)
			.GetService(typeof(IBar<int>));

		// Both wrappers applied; value passes through unchanged
		Assert.Equal(7, result!.Value);
		Assert.IsType<WrappedBar<int>>(result);
	}

	[Fact]
	public void GetService_AppliesBothDecorators_WhenExactAndGenericRegistered()
	{
		// IBar<int> matches both the exact and the generic decorator
		var sp = new SimpleServiceProvider();
		sp.Register<IBar<int>>(new Bar<int>(0));
		var order = new List<string>();

		var result = new DecoratingServiceProvider(sp)
			.Decorate<IBar<int>>((inner, _) => { order.Add("exact"); return inner; })
			.Decorate(typeof(IBar<>), (_, inner, _) => { order.Add("generic"); return inner; })
			.GetService(typeof(IBar<int>));

		Assert.NotNull(result);
		Assert.Equal(["exact", "generic"], order);
	}

	#endregion

	#region Predicate via When

	[Fact]
	public void When_AppliesDecorator_WhenPredicateReturnsTrue()
	{
		var sp = new SimpleServiceProvider();
		sp.Register<IBar<int>>(new Bar<int>(5));

		var result = (IBar<int>?)new DecoratingServiceProvider(sp)
			.When(
				t => t.IsGenericType && t.GetGenericArguments()[0] == typeof(int),
				d => d.Decorate(typeof(IBar<>), (type, inner, _) =>
				{
					var wrapped = typeof(WrappedBar<>).MakeGenericType(type.GetGenericArguments());
					return Activator.CreateInstance(wrapped, inner)!;
				}))
			.GetService(typeof(IBar<int>));

		Assert.IsType<WrappedBar<int>>(result);
	}

	[Fact]
	public void When_SkipsDecorator_WhenPredicateReturnsFalse()
	{
		var sp = new SimpleServiceProvider();
		sp.Register<IBar<int>>(new Bar<int>(5));

		var result = new DecoratingServiceProvider(sp)
			.When(
				_ => false,
				d => d.Decorate(typeof(IBar<>), (_, _, _) => throw new InvalidOperationException("should not apply")))
			.GetService(typeof(IBar<int>));

		Assert.IsType<Bar<int>>(result);
	}

	[Fact]
	public void When_AppliesMultipleDecorators_RegisteredInConfigure()
	{
		var sp = new SimpleServiceProvider();
		sp.Register<IFoo>(new Foo("x"));
		var order = new List<string>();

		new DecoratingServiceProvider(sp)
			.When(
				_ => true,
				d => d.Decorate<IFoo>((inner, _) => { order.Add("first"); return inner; })
					  .Decorate<IFoo>((inner, _) => { order.Add("second"); return inner; }))
			.GetService(typeof(IFoo));

		Assert.Equal(["first", "second"], order);
	}

	[Fact]
	public void When_DoesNotApplyDecoratorsOutsideConfigure_ToOtherTypes()
	{
		var sp = new SimpleServiceProvider();
		sp.Register<IFoo>(new Foo("a"));
		sp.Register<IBar<int>>(new Bar<int>(1));

		var dsp = new DecoratingServiceProvider(sp)
			.When(
				t => t == typeof(IFoo),
				d => d.Decorate<IFoo>((inner, _) => new WrappedFoo(inner)));

		Assert.IsType<WrappedFoo>(dsp.GetService(typeof(IFoo)));
		Assert.IsType<Bar<int>>(dsp.GetService(typeof(IBar<int>)));
	}

	[Fact]
	public void When_RespectsFilter_WhenOpenGenericDecoratorTypeWithPredicate()
	{
		var sp = new SimpleServiceProvider();
		sp.Register<IBar<int>>(new Bar<int>(1));
		sp.Register<IBar<string>>(new Bar<string>("x"));

		var dsp = new DecoratingServiceProvider(sp)
			.When(
				t => t.IsGenericType && t.GetGenericArguments()[0] == typeof(int),
				d => d.Decorate(typeof(IBar<>), typeof(WrappedBar<>)));

		Assert.IsType<WrappedBar<int>>(dsp.GetService(typeof(IBar<int>)));
		Assert.IsType<Bar<string>>(dsp.GetService(typeof(IBar<string>)));
	}

	#endregion

	#region Decorate<TDecorator>() — inferred service type

	[Fact]
	public void GetService_WrapsResolvedService_WhenServiceTypeInferred()
	{
		var sp = new SimpleServiceProvider();
		sp.Register<IFoo>(new Foo("a"));

		var result = (IFoo?)new DecoratingServiceProvider(sp)
			.Decorate<WrappedFoo>()
			.GetService(typeof(IFoo));

		Assert.Equal("wrapped(a)", result!.Name);
	}

	[Fact]
	public void Decorate_ThrowsInvalidOperationException_WhenNoMatchingConstructor()
	{
		Assert.Throws<InvalidOperationException>(() =>
			new DecoratingServiceProvider(new SimpleServiceProvider())
				.Decorate<NoServiceCtorDecorator>());
	}

	[Fact]
	public void GetService_ThrowsInvalidOperationException_WhenDecoratorDoesNotImplementService()
	{
		// Exact path: thrown at Decorator.Exact() call (registration time)
		Assert.Throws<InvalidOperationException>(() =>
			Decorator.Exact(typeof(IFoo), typeof(NotAFooDecorator)));
	}

	[Fact]
	public void GetService_ThrowsInvalidOperationException_WhenOpenGenericDecoratorDoesNotImplementService()
	{
		// Open-generic path: thrown at GetService (first resolve, when the closed type is built)
		var sp = new SimpleServiceProvider();
		sp.Register<IBar<int>>(new Bar<int>(1));

		var dsp = new DecoratingServiceProvider(sp)
			.Decorate(typeof(IBar<>), typeof(BadBarDecorator<>));

		Assert.Throws<InvalidOperationException>(() => dsp.GetService(typeof(IBar<int>)));
	}

	#endregion

	#region Decorate<TService, TDecorator>() — explicit types

	[Fact]
	public void GetService_WrapsResolvedService_WhenExplicitServiceAndDecoratorTypes()
	{
		var sp = new SimpleServiceProvider();
		sp.Register<IFoo>(new Foo("b"));

		var result = (IFoo?)new DecoratingServiceProvider(sp)
			.Decorate<IFoo, WrappedFoo>()
			.GetService(typeof(IFoo));

		Assert.Equal("wrapped(b)", result!.Name);
	}

	[Fact]
	public void GetService_PassesSelfAsServiceProvider_WhenDecoratorAcceptsServiceProvider()
	{
		var sp = new SimpleServiceProvider();
		sp.Register<IFoo>(new Foo("c"));

		var dsp = new DecoratingServiceProvider(sp)
			.Decorate<IFoo, SpCapturingFoo>();

		var result = (SpCapturingFoo?)dsp.GetService(typeof(IFoo));

		Assert.Same(dsp, result!.CapturedSp);
	}

	[Fact]
	public void GetService_WrapsResolvedService_WhenDecoratorHasAdditionalDependencies()
	{
		var sp = new SimpleServiceProvider();
		sp.Register<IFoo>(new Foo("a"));
		sp.Register<IDep>(new Dep("x"));

		var result = (IFoo?)new DecoratingServiceProvider(sp)
			.Decorate<IFoo, FooWithDep>()
			.GetService(typeof(IFoo));

		Assert.Equal("a+x", result!.Name);
	}

	#endregion

	#region Decorate(Type, Type) — open-generic type-based

	[Fact]
	public void GetService_WrapsResolvedService_WhenOpenGenericDecoratorType()
	{
		var sp = new SimpleServiceProvider();
		sp.Register<IBar<int>>(new Bar<int>(42));

		var result = (IBar<int>?)new DecoratingServiceProvider(sp)
			.Decorate(typeof(IBar<>), typeof(WrappedBar<>))
			.GetService(typeof(IBar<int>));

		Assert.IsType<WrappedBar<int>>(result);
		Assert.Equal(42, result.Value);
	}

	[Fact]
	public void GetService_UsesCachedFactory_WhenCalledMultipleTimes()
	{
		var sp = new SimpleServiceProvider();
		sp.Register<IBar<int>>(new Bar<int>(7));
		var dsp = new DecoratingServiceProvider(sp).Decorate(typeof(IBar<>), typeof(WrappedBar<>));

		var r1 = dsp.GetService(typeof(IBar<int>));
		var r2 = dsp.GetService(typeof(IBar<int>));

		Assert.IsType<WrappedBar<int>>(r1);
		Assert.IsType<WrappedBar<int>>(r2);
	}

	#endregion
}
