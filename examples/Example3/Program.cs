using Logger;
using RSG.Factory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//
// This example shows how to factory create an object by specifying an interface of the object's type.
// 
namespace Example
{
    public interface IMyType
    {
        string Message { get; }
    }

    public class MyType : IMyType
    {
        public string Message
        {
            get
            {
                return "Hello Computer";
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var factory = new Factory("MyApp", new MyLogger());

            // Register the type.
            factory.Type<IMyType>(typeof(MyType));

            // Create an instance.
            var myFactoryCreatedObject = factory.CreateInterface<IMyType>();

            Console.WriteLine(myFactoryCreatedObject.Message);
        }
    }
}
