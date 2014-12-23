using Logger;
using RSG.Factory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//
// This example shows how to inject property dependencies into a factory created object.
// 
namespace Example
{
    public interface IMyDependency
    {
        string Message { get; }
    }

    public class MyDependency : IMyDependency
    {
        public string Message
        {
            get
            {
                return "From the property dependency!";
            }
        }
    }

    public interface IMyType
    {
        string Message { get; }
    }

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

            // Register a dependency.
            factory.Dep<IMyDependency>(new MyDependency());

            // Register the type.
            factory.Type<IMyType>(typeof(MyType));

            // Create an instance with dependency injected.
            var myFactoryCreatedObject = factory.CreateInterface<IMyType>();

            Console.WriteLine(myFactoryCreatedObject.Message);
        }
    }
}
