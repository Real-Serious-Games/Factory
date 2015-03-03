using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RSG
{
    /// <summary>
    /// Attribute that marks a class as being creatable by the factory.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class FactoryCreatableAttribute : Attribute
    {
        public string TypeName
        {
            get;
            private set;
        }

        public FactoryCreatableAttribute()
        {
        }

        public FactoryCreatableAttribute(string typeName)
        {
            this.TypeName = typeName;
        }

        public FactoryCreatableAttribute(Type interfaceType)
        {
            this.TypeName = Factory.GetTypeName(interfaceType);
        }

        public FactoryCreatableAttribute(Type viewType, Type dataInterfaceType)
        {
            this.TypeName = viewType.Name + "_" + dataInterfaceType.Name;
        }
    }
}
