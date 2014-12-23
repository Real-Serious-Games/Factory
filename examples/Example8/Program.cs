using Logger;
using RSG.Factory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//
// This example shows how to auto register a type created by interface using the FactoryCreatable attribute.
// 
namespace Example
{
    public interface IMyType
    {
        string Message { get; }
    }

    [FactoryCreatable(typeof(IMyType))] // This attribute identifies the type as factory createable.
    public class MyType : IMyType
    {
        public string Message
        {
            get
            {
                return "My auto registered factory type!";
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var factory = new Factory("MyApp", new MyLogger());

            // Auto register types.
            factory.AutoRegisterTypes();

            // Create an instance.
            var myFactoryCreatedObject = factory.CreateInterface<IMyType>();

            Console.WriteLine(myFactoryCreatedObject.Message);
        }
    }
}
