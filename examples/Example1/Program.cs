using Logger;
using RSG;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//
// This example shows how to factory create an object by specifying the name of the object's type.
// 
namespace Example
{
    public class MyType
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
            factory.Type("MyType", typeof(MyType));

            // Create an instance.
            var myFactoryCreatedObject = factory.Create<MyType>("MyType");

            Console.WriteLine(myFactoryCreatedObject.Message);
        }
    }
}
