using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Utils
{
    /// <summary>
    /// Attribute that defines a lazily creatable singleton.
    /// </summary>
    public class LazySingletonAttribute : FactoryCreatableAttribute
    {
        public LazySingletonAttribute(string name) :
            base(name)
        {
        }

        public LazySingletonAttribute(Type interfaceType) :
            base(interfaceType)
        {
        }
    }
}
