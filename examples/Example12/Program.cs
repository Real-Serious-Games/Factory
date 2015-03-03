using Logger;
using RSG;
using RSG.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//
// This example shows how to dependency inject a lazy singletons.
// Lazy singletons are only created just before they are dependency injected.
// 
namespace Example
{
    public interface IMyLazySingleton
    {
        string Message { get; }
    }

    [LazySingleton(typeof(IMyLazySingleton))] // This attribute idenfies the type as a lazy singleton.
    public class MyLazySingleton : IMyLazySingleton
    {
        public string Message 
        { 
            get
            {
                return "This is my lazy singleton!";
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
        public IMyLazySingleton MyLazySingleton { get; set; }

        public string Message
        {
            get
            {
                return MyLazySingleton.Message;
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

            // Normal singleton bootstrapping... although the lazy singleton isn't instantiated yet.
            factory.AutoInstantiateSingletons();

            // Create an instance.
            // This will instantiate the lazy singleton at the point when it is requested by the dependency injection system.
            var myFactoryCreatedObject = factory.CreateInterface<IMyType>();

            Console.WriteLine(myFactoryCreatedObject.Message);
        }
    }
}
