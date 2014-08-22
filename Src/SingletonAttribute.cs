using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Utils
{
    /// <summary>
    /// Attribute that defines a singleton created at startup.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class SingletonAttribute : Attribute
    {
        /// <summary>
        /// Interface for dependency injection of the singleton.
        /// </summary>
        public Type InterfaceType { get; private set; }

        public SingletonAttribute()
        {
        }

        public SingletonAttribute(Type interfaceType)
        {
            this.InterfaceType = interfaceType;
        }

        public override string ToString()
        {
            if (InterfaceType != null)
            {
                return "Singleton implementing: " + InterfaceType.Name;
            }
            else
            {
                return "Singleton";
            }
        }
    }
}
