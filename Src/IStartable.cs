using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RSG.Factory
{
    /// <summary>
    /// Interface for an object that can be started and shutdown.
    /// </summary>
    public interface IStartable
    {
        /// <summary>
        /// Start the object.
        /// </summary>
        void Start();

        /// <summary>
        /// Shutdown the object.
        /// </summary>
        void Shutdown();
    }
}
