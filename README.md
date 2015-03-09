# Factory [![Build Status](https://travis-ci.org/Real-Serious-Games/Factory.svg)](https://travis-ci.org/Real-Serious-Games/Factory)

Easy to use C# factory/IOC-container for object creation and dependency injection.

Used by Real Serious Games in serious games built with [Unity3D](http://unity3d.com/). 

## Recent Updates

- 3 March 2015: v1.1.0.0
  - Breaking changes:
    - Namespace has been changed from `RSG.Factory` to `RSG`, to avoid C#'s problems with classes having the same name as the namespace.
- 27 Feb 2015: v1.0.0.7
  - Singleton setup is now simplified and easier to use. See this docs or included examples for details.
  - IStartable *Start* has been renamed to *Startup* to avoid conflicts with the MonoBehaviour *Start* function for Unity scripts. 

## Contents

<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->
**Table of Contents**  *generated with [DocToc](https://github.com/thlorenz/doctoc)*

- [Getting Started](#getting-started)
  - [Getting the DLL](#getting-the-dll)
  - [Getting the Code](#getting-the-code)
  - [Factory Setup](#factory-setup)
- [Creating Objects](#creating-objects)
  - [Factory Creation by Name](#factory-creation-by-name)
  - [Factory Creation by Interface](#factory-creation-by-interface)
  - [Constructor Arguments](#constructor-arguments)
  - [Automatic Setup for Factory Creation](#automatic-setup-for-factory-creation)
- [Dependency Injection](#dependency-injection)
  - [Dependency Injection via Properties](#dependency-injection-via-properties)
  - [Dependency Injection via Constructor Arguments](#dependency-injection-via-constructor-arguments)
- [Dependency Injection Setup](#dependency-injection-setup)
  - [Manual Setup for Dependency Injection](#manual-setup-for-dependency-injection)
  - [Automatic Setup for Dependency Injection](#automatic-setup-for-dependency-injection)
- [Singletons](#singletons)
  - [Singleton Instantiation](#singleton-instantiation)
  - [Singletons can depend on other singletons!](#singletons-can-depend-on-other-singletons)
  - [Singleton Startup/Shutdown](#singleton-startupshutdown)
  - [Custom Singleton Instantiation](#custom-singleton-instantiation)
- [Examples](#examples)
  - [Example C# Projects](#example-c#-projects)

<!-- END doctoc generated TOC please keep comment here to allow auto update -->

## Getting Started

### Getting the DLL

The DLL can be installed via nuget. Use the Package Manager UI or console in Visual Studio or use nuget from the command line.

See here for instructions on installing a package via nuget: http://docs.nuget.org/docs/start-here/using-the-package-manager-console

The package to search for is *RSG.Factory*.

### Getting the Code

You can get the code by cloning the github repository. You can do this in a UI like SourceTree or you can do it from the command line as follows:

	git clone https://github.com/Real-Serious-Games/Factory.git

Alternately, to contribute please fork the project in github.

### Factory Setup

Reference the DLL and import the namespace:

	using RSG; 

Instantiate the factory as follows:
	
	Factory factory = new Factory("MyApp", new MyLogger());

The first parameter specifies a name of the factory instance and is for debugging only. Naming factorys helps when using multiple factories.

The second parameter is an instance of an object that implements `ILogger`. This class must be implemented by you, for an example of the simplest possible implementation see the included example projects.

## Creating Objects

### Factory Creation by Name

The simplest use of the factory is to create objects by name.

Start by defining a type to create:

    public class MyType
    {
    }

Then register the type with the factory:

    factory.Type("MyType", typeof(MyType));

Now you can create instance of the type by specifying the type name:

    MyType myFactoryCreatedObject = factory.Create<MyType>("MyType");

This technique is useful in [data-driven programming](http://stackoverflow.com/questions/1065584/what-is-data-driven-programming).

### Factory Creation by Interface

Objects can also be created by specifying their interface.

This time, the type has an interface:

	public interface IMyType
    {
    }

    public class MyType : IMyType
    {
    }

Now register the type with the factory:

    factory.Type<IMyType>(typeof(MyType));

    
And create instances by specifying the interface:

    IMyType myFactoryCreatedObject = factory.CreateInterface<IMyType>();

This technique is useful in [test-driven development](http://en.wikipedia.org/wiki/Test-driven_development) for [mocking](http://en.wikipedia.org/wiki/Mock_object) the interface of the object being created.

### Constructor Arguments

Any number of constructor arguments can be specified when creating objects, provided there actually is a constructor that takes the arguments.

By name:

	MyType myFactoryCreatedObject = factory.Create<MyType>("MyType", 1, 2, "3");

Or by interface:

	IMyType myFactoryCreatedObject = factory.CreateInterface<IMyType>(1, 2, "3");

This assumes that MyType has a public constructor with appropriate parameters, such as:

	public MyType(int a1, int a2, string a3)
	{
		...
	}

If no such constructor exists an exception will be thrown at run-time when attempting to create the object.

This highlights the downside of using the factory, you don't get have compile-time checking of constructor arguments the way you would if you were just *newing* 
objects directly, however I believe that being able to use TDD outweights this issue. If you ever come up with a good solution to this problem **please** 
let us know!

### Automatic Setup for Factory Creation

Registering types manually is boring work. Here's how to do it automatically.

Use the *FactoryCreatable* [attribute](http://stackoverflow.com/questions/20346/net-what-are-attributes) to mark your types.

By name:

	[FactoryCreatable]
	public class MyType
    {
    }

Custom name:

	[FactoryCreatable("MyCustomName")]
	public class MyType
    {
    }

Or by interface:

	[FactoryCreatable(typeof(IMyType))]
    public class MyType : IMyType
    {
    }

Then ask the factory to scan loaded DLLs for marked types:

	factory.AutoRegisterTypes();

Be careful, this can be **expensive**! Ideally you will only do this only once at startup.

You can now create the automatically registered objects by name or interface as necessary.

## Dependency Injection

### Dependency Injection via Properties

Dependencies can be injected (http://en.wikipedia.org/wiki/Dependency_injection) for properties as follows:

	[FactoryCreatable]
    public class MyType
    {
		[Dependency]
		public IMyDependency MyDependency { get; set; }
    }

The `Dependency` attribute marks properties to be resolved. After the object has been factory-created the dependencies will be satisfied.

Properties must have a public *getter* and *setter*.

Property dependencies are very convenient (although it's a trade-off against the bad design of using public *setters*), but the properties can't be used in constructors. 
This is because property dependencies, by necessity, are resolved after the object is constructed, therefore after the constructor has executed. 
To use use injected dependencies in a constructor, they must be injected as constructor arguments.

### Dependency Injection via Constructor Arguments

Constructor argument injection is cleaner than property injection (as it eliminates public *setters*), but it is less convenient, but you must do it this way to use dependencies in the constructor.

An example:

	[FactoryCreatable]
    public class MyType
    {
		public MyType(IMyDependency myDependency)
		{
			...
		}
    }

Dependency injected constructor arguments can be used with normal constuctor arguments, provided the normal arguments come first:

	[FactoryCreatable]
    public class MyType
    {
		public MyType(int a, string b, IMyDependency myDependency)
		{
			...
		}
    }

Any number of injected arguments can come after any number of normal arguments.

Remember that when you factory-create an object that takes injected constructor arguments that you shouldn't manually provide those arguments. You could do that if you wanted to, and maybe that's
warranted in some scenarios, but generally don't do it because it defeats the point of having automatic injection.

## Dependency Injection Setup

### Manual Setup for Dependency Injection

Dependencies must be registered with the factory before they can be injected.

This can be done manually as follows:

	factory.Dep<IMyDependency>(new MyDependency());

This registers an instance of `MyDependency` and associates it with the interface `IMyDependency`. Whenever `IMyDependency` is requested
as a dependency it will resolve to the specified `MyDependency` object.

### Automatic Setup for Dependency Injection

Again... manual setup is boring and time consuming. Enjoy some automatic setup...

Any class that has been marked with `FactoryCreatable` (the interface version) will also be dependency-injectable:

	[FactoryCreatable(typeof(IMyDependency))]
	public class MyDependency : IMyDependency
	{
		...
	}

Whenever `IMyDependency` is requested as a dependency it will automatically resolve to an instance `MyDependency`. A new instance is created each time. 
See the next section if you need to dependency inject singletons, that is objects that are only created once and shared each time the dependency is needed.

## Singletons

### Singleton Instantiation

Classes marked with the *Singleton* attribute are automatically detected, instantiated and made ready for dependency injection. As these objects are [singletons](http://en.wikipedia.org/wiki/Singleton_pattern) there is only a single instance that is shared to all objects that requested the dependency.

This is how you define a singleton:

	[Singleton(typeof(IMySingleton))]
	public class MySingleton : IMySingleton
	{
		...
	}

The parameter to the *Singleton* attribute specifies the type that is injected. The singleton is injected into other objects by requesting the singleton's interface as a constructor parameter or a property dependency (as documented earlier).

The factory is instructed to find and instantiate singletons as follows:

	factory.AutoInstantiateSingletons();

At this point any class marked with *Singleton* will have been instantiated and is ready for dependency injection.

In some circumstances you will want to delay instantiation of singletons until the time when the object is requested for dependency injection for the first time. We call these *lazy singletons* and you can mark them using the *LazySingleton* attribute:  

	[LazySingleton(typeof(IMyLazySingleton))]
	public class MyLazySingleton : IMyLazySingleton
	{
		...
	}

### Singletons can depend on other singletons!

Singletons can have other singletons injected as dependencies (in addition to regular dependencies): 

	[Singleton(typeof(IMySingleton1))]
	public class MySingleton1 : IMySingleton1
	{
		...
	}

	[Singleton(typeof(IMySingleton2))]
	public class MySingleton2 : IMySingleton2
	{
		[Dependency]
		public IMySingleton1 MySingleton1 { get; set; }

		...
	}

The system automatically sorts *singleton definitions* by dependency prior to instantiation. This ensures that for any give singleton being instantiated, other singletons it depends on will already have been instantiated.

Tweaking the startup order of objects in your application can be a painful experience, but automatic ordering of dependencies makes it so much easier! 

Circular references between singletons are illegal for obvious reasons, a nasty exception will be thrown at you if you try to do that.

### Singleton Startup/Shutdown    

Singletons that implement *IStartable* can have their *Startup* and *Shutdown* methods called.

 	[Singleton(typeof(IMySingleton))]
	public class MySingleton : IMySingleton, IStartable
	{
		public void Startup()
		{
			// ... initalization logic
		}

		public void Shutdown()
		{
			// ... shutdown logic
		}
	}

Calling *AutoInstantiateSingletons* returns an *ISingletonManager* that can be used to interact with your singletons, for example when starting the application: 

	var singletonManager = factory.AutoInstantiateSingletons();
	singletonManager.Startup();

And when shutting down your application:

	singletonManager.Shutdown();

You can interact generally with singleton objects via the *Singletons* property. For example, on shutdown you might want to query all singletons to ask them if its ok to shutdown given the current state of the application, let's say we are using an interface you have defined called *IQueryShutdown*:

    var toQuery = singletonManager.Singletons
		.Where(singleton => singleton is IQueryShutdown)
        .Cast<IQueryShutdown>();

	foreach (var singleton in toQuery)
	{
		if (!singleton.CanShutdown())
		{
			// The singleton wants to prevent shutdown for some reason!
			...
		}
	}

### Custom Singleton Instantiation

You can customize singleton instantiation by [creating your own attribute](https://msdn.microsoft.com/en-us/library/sw480ze8.aspx) that derives from *SingletonAttribute* and implements *CreateInstance*.

Here is a basic example:
 
	public class MySpecialSingletonAttribute : SingletonAttribute
	{
		... constructors ...

		public override object CreateInstance(IFactory factory, Type type)
		{
			... instantiate your special singleton and return it ...
		}
	}

At [Real Serious Games](https://github.com/Real-Serious-Games) we have a special attribute for creating our Unity singletons... because they need special instantiation logic. So if you want to see a real example please see our *UnitySingletonAttribute* in the *[RSG.Unity](https://github.com/Real-Serious-Games/RSG.Unity)* project.

## Examples

### Example C# Projects

This project comes with numerous example projects. I recommend cloning to browse locally or just browsing via the github web interface.






