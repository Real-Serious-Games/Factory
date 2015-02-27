using Logger;
using RSG.Factory;
using RSG.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//
// This example shows how to start and shutdown a singleton.
// 
namespace Example
{
    public interface IMySingleton
    {
        string Message { get; }
    }

    [Singleton(typeof(IMySingleton))] // This attribute idenfies the type as an injectable singleton.
    public class MySingleton : IMySingleton, IStartable  // Implements IStartable to have Start and Shutdown called.
    {
        public string Message 
        { 
            get
            {
                return "This is my startable singleton!";
            }
        }

        public void Startup() // Called when the singleton is started.
        {
            Console.WriteLine("My singleton has started!");
        }

        public void Shutdown() // Called when the singleton is shutdown.
        {
            Console.WriteLine("My singleton has shutdown!");
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

            // Auto bootstrap singletons for classes marked as [Singleton].
            var singletonManager = factory.AutoInstantiateSingletons();

            // Start-up startable singletons.
            singletonManager.Start();

            // Create an instance.
            var myFactoryCreatedObject = factory.CreateInterface<IMyType>();

            Console.WriteLine(myFactoryCreatedObject.Message);

            // Shutdown startable singletons.
            singletonManager.Shutdown();
        }
    }
}
