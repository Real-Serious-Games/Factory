using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RSG.Factory
{
    /// <summary>
    /// Attribute that defines a singleton created at startup.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class SingletonAttribute : Attribute
    {
        /// <summary>
        /// Name for dependency injection of the singleton.
        /// </summary>
        public string DependencyName { get; private set; }

        public SingletonAttribute()
        {
        }

        public SingletonAttribute(Type interfaceType)
        {
            this.DependencyName = interfaceType.Name;
        }

        public SingletonAttribute(string dependencyName)
        {
            this.DependencyName = dependencyName;
        }

        public override string ToString()
        {
            if (DependencyName != null)
            {
                return "Singleton available as dependency: " + DependencyName;
            }
            else
            {
                return "Singleton";
            }
        }

        public virtual bool Lazy
        {
            get
            {
                return false;
            }
        }
    }
}
