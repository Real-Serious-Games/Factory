Factory [![Build Status](https://travis-ci.org/Real-Serious-Games/Factory.svg)](https://travis-ci.org/Real-Serious-Games/Factory)
=======

Easy to use C# factory/IOC-container for object creation and dependency injection.

Getting the DLL
---------------

The DLL can be installed via nuget. Use the Package Manager UI or console in Visual Studio or use nuget from the command line.

See here for instructions on installing a package via nuget: http://docs.nuget.org/docs/start-here/using-the-package-manager-console

Getting the Code
----------------

You can get the code by cloning the github repository. You can do this in a UI like SourceTree or you can do it from the command line as follows:

	git clone https://github.com/Real-Serious-Games/Factory.git

Alternately if you want to contribute you fork the project in github.

Factory Setup 
-------------

After referencing the DLL and ''using'' the namespace, the factory is instantiated as follows:
	
	Factory factory = new Factory("MyApp", new MyLogger());

The first parameter specifies a name of the factory instance and is for debugging only. Naming factorys will help if you use multiple factories.

The second parameter is an instance of an object that implements the ILogger interface. The class that implements ILogger is defined by the you, for an example of 
the simplest possible implementation see the included example projects.

Factory Creation by Name
------------------------

The simplest use of the factory is to create objects by name.

Start by defining a type to create:

    public class MyType
    {
    }

Then register the type with the factory:

    factory.Type("MyType", typeof(MyType));

Now you can create instance of the type by specifying the type name:

    MyType myFactoryCreatedObject = factory.Create<MyType>("MyType");


This technique is useful for data-driven programming (http://stackoverflow.com/questions/1065584/what-is-data-driven-programming).


Factory Creation by Interface
-----------------------------

Objects can also be created by specifying their interface.

This time, the type we are going to create must have an interface:

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

This technique is useful for test-driven development (http://en.wikipedia.org/wiki/Test-driven_development) 
when you want to mock (http://en.wikipedia.org/wiki/Mock_object) the interfaces of the objects being created.


Constructor Arguments
---------------------

Any number of constructor arguments can be specified when creating objects, provided there is actually is a constructor that takes the arguments.

By name:

	MyType myFactoryCreatedObject = factory.Create<MyType>("MyType", 1, 2, "3");

Or by interface:

	IMyType myFactoryCreatedObject = factory.CreateInterface<IMyType>(1, 2, "3");

This assumes that MyType has a public constructor with appropriate parameters, like:

	public MyType(int a1, int a2, string a3)
	{
		...
	}

If no such constructor exists an exception will be thrown at run-time when attempting to create the object.

This is a downside of using the factory, you don't get have compile-time checking of constructor arguments the way you would if you were just 'newing' 
objects directly, however I believe that being able to use TDD outweights this issue. If you ever come up with a good solution to this problem please 
let us know!

Automatic Setup for Factory Creation
------------------------------------

Registering types manually is boring work. Here's how to do it automatically.

By name...

Use the 'FactoryCreatable' attribute to mark your type:

	[FactoryCreatable]
	public class MyType
    {
    }

Or if you using an interface: 

	[FactoryCreatable(typeof(IMyType))]
    public class MyType : IMyType
    {
    }

Scan loaded DLLs for marked types:

	factory.AutoRegisterTypes();


Be careful, this can be expensive! Ideally you will only do this once at startup.

Then create objects by name or interface as necessary.

Dependency Injection via Properties
-----------------------------------

Dependencies can be injected (http://en.wikipedia.org/wiki/Dependency_injection) for properties as follows:

	[FactoryCreatable]
    public class MyType
    {
		[Dependency]
		public IMyDependency MyDependency { get; set; }
    }

The 'Dependency' attribute marks the properties to be resolved. After the object has been factory created the dependencies will be satisfied.

Properties must have a 'setter'.

Property dependencies are very convenient (although it's a trade off against slightly bad design with the public setters) by they can't be used in constructors. 
That is because property dependencies, by necessity, are resolved after the object is constructed. If you want to use use dependencies in the constructor, 
they must be injected as constructor arguments.


Dependency Injection via Constructor Arguments
----------------------------------------------

Constructor argument injection is cleaner than property injection (as it eliminates public setters) but it is less convenient.

It is necessary when you need to access dependencies with in the constructor.

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

Manual Setup for Dependency Injection
-------------------------------------

Dependencies must be register before they can be injected.

This can be done manually as follows:

	factory.Dep<IMyDependency>(new MyDependency());

This registers an instance of `MyDependency` and associates it with the interface `IMyDependency`. Whenever `IMyDependency` is requested
as a dependency it will resolve to the specified `MyDependency` object.


Automatic Setup for Depency Injection
-------------------------------------

Again... manual setup is boring and time consuming. Have some automatic setup.

Any class that has been marked with the interface version of `FactoryCreatable` will also be dependency-injectable:

	[FactoryCreatable(typeof(IMyDependency))]
	public class MyDependency : IMyDependency
	{
		...
	}

Whenever `IMyDependency` is requested as a dependency it will automatically resolve to an instance `MyDependency`. A new instance is created each time. 
See the next section if you need to dependency inject singletons, that is objects that are only created once and shared each time the dependency is needed.

Singleton Setup
---------------

Coming soon!


Example Projects
----------------

This project comes with numerous example projects. I recommend cloning to browse locally or just browsing via the github web interface.






