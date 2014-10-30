using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RSG.Factory
{
    /// <summary>
    /// Attribute that defines a lazily creatable singleton.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class LazySingletonAttribute : SingletonAttribute
    {
        public LazySingletonAttribute(string dependencyName) :
            base(dependencyName)
        {
        }

        public LazySingletonAttribute(Type interfaceType) :
            base(interfaceType)
        {
        }

        public override string ToString()
        {
            return "LazySingleton available as dependency: " + DependencyName;
        }

        public override bool Lazy
        {
            get
            {
                return true;
            }
        }
    }
}
