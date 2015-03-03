using Logger;
using RSG;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//
// This example shows how to factory create an instance of object by interface and passing in constructor arguments.
// 
namespace Example
{
    public interface IMyType
    {
        string Message { get; }
    }

    public class MyType : IMyType
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
            factory.Type<IMyType>(typeof(MyType));

            // Create an instance and pass in constructor arguments.
            var myFactoryCreatedObject = factory.CreateInterface<IMyType>("Hello", "Factory");

            Console.WriteLine(myFactoryCreatedObject.Message);
        }
    }
}
