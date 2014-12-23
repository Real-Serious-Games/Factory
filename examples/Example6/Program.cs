using Logger;
using RSG.Factory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//
// This example shows how to inject constructor argument dependencies into a factory created object.
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
                return "From the constructor argument dependency!";
            }
        }
    }

    public interface IMyType
    {
        string Message { get; }
    }

    public class MyType : IMyType
    {
        private IMyDependency myDependency;

        public MyType(IMyDependency myDependency)
        {
            this.myDependency = myDependency;
        }

        public string Message
        {
            get
            {
                return myDependency.Message;
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
