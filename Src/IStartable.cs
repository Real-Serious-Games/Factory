using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RSG
{
    /// <summary>
    /// Interface for an object that can be started and shutdown.
    /// </summary>
    public interface IStartable
    {
        /// <summary>
        /// Start the object.
        /// </summary>
        void Startup();

        /// <summary>
        /// Shutdown the object.
        /// </summary>
        void Shutdown();
    }
}
