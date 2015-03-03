using Logger;
using RSG;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//
// This example shows how to auto register a named type using the FactoryCreatable attribute.
// 
namespace Example
{
    [FactoryCreatable] // This attribute identifies the type as factory createable.
    public class MyType
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
            var myFactoryCreatedObject = factory.Create<MyType>("MyType");

            Console.WriteLine(myFactoryCreatedObject.Message);
        }
    }
}
