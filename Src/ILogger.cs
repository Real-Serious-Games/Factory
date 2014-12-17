using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RSG.Utils
{
    /// <summary>
    /// Interface for logging errors.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Enables verbose logging.
        /// </summary>
        bool EnableVerbose { get; set; }

        /// <summary>
        /// Log an error.
        /// </summary>
        void LogError(string message);

        /// <summary>
        /// Log an error.
        /// </summary>
        void LogError(Exception ex, string message);

        /// <summary>
        /// Log an info message.
        /// </summary>
        void LogInfo(string message);

        /// <summary>
        /// Log an info message.
        /// </summary>
        void LogInfo(string message, params object[] args);

        /// <summary>
        /// Log a warning message.
        /// </summary>
        void LogWarning(string message);

        /// <summary>
        /// Log a verbose message, by default these aren't output.
        /// </summary>
        void LogVerbose(string message);

        /// <summary>
        /// Indent verbose log messages.
        /// </summary>
        void Indent();

        /// <summary>
        /// Unindent verbose log messages.
        /// </summary>
        void Unindent();
    }
}
