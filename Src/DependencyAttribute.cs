using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RSG
{
    /// <summary>
    /// Used to mark properties for dependency injection.
    /// </summary>
    public class DependencyAttribute : Attribute
    {
        public DependencyAttribute()
        {
        }

        public DependencyAttribute(Type dependencyType)
        {
            this.DependencyType = dependencyType;
        }

        /// <summary>
        /// The type of the dependency to inject.
        /// </summary>
        public Type DependencyType { get; private set; }
    }
}
