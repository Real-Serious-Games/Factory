using Logger;
using RSG.Factory;
using RSG.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//
// This example shows how to auto register and dependency inject a singleton.
// 
namespace Example
{
    public interface IMySingleton
    {
        string Message { get; }
    }

    [Singleton(typeof(IMySingleton))] // This attribute idenfies the type as an injectable singleton.
    public class MySingleton : IMySingleton
    {
        public string Message 
        { 
            get
            {
                return "This is my singleton!";
            }
        }

    }

    public interface IMyType
    {
        string Message { get; }
    }

    [FactoryCreatable(typeof(IMyType))] // This attribute identifies the type as factory createable.
    public class MyType : IMyType
    {
        [Dependency] // Dependency inject the singleton.
        public IMySingleton MySingleton { get; set; }

        public string Message
        {
            get
            {
                return MySingleton.Message;
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var logger = new MyLogger();
            var factory = new Factory("MyApp", logger);

            // Auto register types.
            factory.AutoRegisterTypes();

            var reflection = new Reflection();
            var singletonManager = new SingletonManager(reflection, logger, factory);
            var singletonScanner = new SingletonScanner(reflection, logger, singletonManager);

            // Connect the singleton manager to the factory so that it can be used to satisify dependencies.
            factory.AddDependencyProvider(singletonManager);

            // Auto register singletons whose types are marked with the Singleton attribute.
            singletonScanner.ScanSingletonTypes();

            // Instantiate singletons.
            singletonManager.InstantiateSingletons(factory);

            // Create an instance.
            var myFactoryCreatedObject = factory.CreateInterface<IMyType>();

            Console.WriteLine(myFactoryCreatedObject.Message);
        }
    }
}
