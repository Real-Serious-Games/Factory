using Logger;
using RSG.Factory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//
// This example shows how to factory create an object by name and passing in constructor arguments.
// 
namespace Example
{
    public class MyType
    {
        private string msg1;
        private string msg2;
        public MyType(string msg1, string msg2)

        {
            this.msg1 = msg1;
            this.msg2 = msg2;
        }


        public string Message
        {
            get
            {
                return msg1 + " " + msg2 + "!";
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

            // Create an instance and pass in constructor arguments.
            var myFactoryCreatedObject = factory.Create<MyType>("MyType", "Hello", "Computer");

            Console.WriteLine(myFactoryCreatedObject.Message);
        }
    }
}
