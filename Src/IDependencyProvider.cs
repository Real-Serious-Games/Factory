using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Utils
{
    /// <summary>
    /// Interface that can provide dependencies.
    /// </summary>
    public interface IDependencyProvider
    {
        /// <summary>
        /// Retreive a named dependency.
        /// Return null if the dependency was not found.
        /// </summary>
        object FindDependency(string name);
    }
}
