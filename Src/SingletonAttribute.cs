using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RSG
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

        /// <summary>
        /// Whether or not the class should be enabled for discovery in the current build
        /// </summary>
        public virtual bool Discoverable()
        {
            if (discoverablePredicate != null)
            {
                return discoverablePredicate();
            }
            return true;
        }

        private readonly Func<bool> discoverablePredicate;

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

        public SingletonAttribute(Func<bool> discoverablePredicate)
        {
            this.discoverablePredicate = discoverablePredicate;
        }

        public SingletonAttribute(Type interfaceType, Func<bool> discoverablePredicate)
        {
            this.DependencyName = interfaceType.Name;
            this.discoverablePredicate = discoverablePredicate;
        }

        public SingletonAttribute(string dependencyName, Func<bool> discoverablePredicate)
        {
            this.DependencyName = dependencyName;
            this.discoverablePredicate = discoverablePredicate;
        }

        /// <summary>
        /// Create the singleton instance. Override to to provide custom creation logic.
        /// </summary>
        public virtual object CreateInstance(IFactory factory, Type type)
        {
            return factory.Create(type);
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
