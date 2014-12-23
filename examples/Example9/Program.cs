using Logger;
using RSG.Factory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//
// This example shows how to mark dependencies with the FactoryCreatable attribute so they are automatically resolved when they need to be injected.
// 
namespace Example
{
    public interface IMyDependency
    {
        string Message { get; }
    }

    [FactoryCreatable(typeof(IMyDependency))] // With this attribute the depenency will now be automatically resolved.
    public class MyDependency : IMyDependency
    {
        public string Message
        {
            get
            {
                return "My auto resolved dependency!";
            }
        }
    }

    public interface IMyType
    {
        string Message { get; }
    }

    [FactoryCreatable(typeof(IMyType))]
    public class MyType : IMyType
    {
        [Dependency]
        public IMyDependency MyDependency { get; set; }

        public string Message
        {
            get
            {
                return MyDependency.Message;
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var factory = new Factory("MyApp", new MyLogger());

            // Auto register types and dependencies.
            factory.AutoRegisterTypes();

            // Create an instance with dependency injected.
            var myFactoryCreatedObject = factory.CreateInterface<IMyType>();

            Console.WriteLine(myFactoryCreatedObject.Message);
        }
    }
}
