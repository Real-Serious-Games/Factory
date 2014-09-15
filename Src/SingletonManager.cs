using Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utils.Dbg;

namespace Utils
{
    /// <summary>
    /// Defines a singleton.
    /// </summary>
    public struct SingletonDef
    {
        /// <summary>
        /// The type of the singleton.
        /// </summary>
        public Type singletonType;

        /// <summary>
        /// Names of dependencies that the singleton satisfies.
        /// </summary>
        public string[] dependencyNames;
    }

    /// <summary>
    /// Interface to the singleton manager (for mocking).
    /// </summary>
    public interface ISingletonManager
    {
        /// <summary>
        /// Register a singleton with the singleton manager.
        /// The singleton will be instantiated when InstantiateSingletons is called.
        /// </summary>
        void RegisterSingleton(SingletonDef singletonDef);
    }

    /// <summary>
    /// Manages singletons.
    /// </summary>
    public class SingletonManager : ISingletonManager
    {
        /// <summary>
        /// Singletons that are loaded.
        /// </summary>
        public object[] Singletons { get; private set; }

        /// <summary>
        /// For creating objects.
        /// </summary>
        private IFactory factory;

        /// <summary>
        /// For mockable C# reflection services.
        /// </summary>
        private IReflection reflection;

        /// <summary>
        /// Map that allows singletons to be dependency injected.
        /// Maps 'dependency name' to singleton object.
        /// </summary>
        private Dictionary<string, object> dependencyCache = new Dictionary<string, object>();

        /// <summary>
        /// For logging.
        /// </summary>
        private ILogger logger;

        /// <summary>
        /// Definitions for all known (non-lazy) singletons.
        /// </summary>
        private List<SingletonDef> singletonDefs = new List<SingletonDef>();

        public SingletonManager(IFactory factory, IReflection reflection, ILogger logger)
        {
            Argument.NotNull(() => factory);
            Argument.NotNull(() => reflection);
            Argument.NotNull(() => logger);

            this.factory = factory;
            this.reflection = reflection;
            this.logger = logger;
            this.Singletons = new object[0];
        }

        /// <summary>
        /// Register a singleton with the singleton manager.
        /// The singleton will be instantiated when InstantiateSingletons is called.
        /// </summary>
        public void RegisterSingleton(SingletonDef singletonDef)
        {
            Argument.NotNull(() => singletonDef);
            Argument.NotNull(() => singletonDef.singletonType);

            singletonDefs.Add(singletonDef);
        }

        /// <summary>
        /// Find a dependency to a singleton by name.
        /// Returns null if the singleton doesn't exist.
        /// </summary>
        public object ResolveDependency(string dependencyName)
        {
            Argument.StringNotNullOrEmpty(() => dependencyName);

            object singleton;
            if (!dependencyCache.TryGetValue(dependencyName, out singleton))
            {
                return null;
            }

            return singleton;
        }

        /// <summary>
        /// Instantiate all known (non-lazy) singletons.
        /// </summary>
        public void InstantiateSingletons()
        {
            Singletons = singletonDefs
                .Select(InstantiateSingleton)
                .ToArray();
        }

        /// <summary>
        /// Instantiate a singleton from a definition.
        /// </summary>
        private object InstantiateSingleton(SingletonDef singletonDef)
        {
            var type = singletonDef.singletonType;
            logger.LogInfo("Creating singleton: " + type.Name);

            // Instantiate the singleton.
            var singleton = factory.Create(type);
            singletonDef.dependencyNames.Each(dependencyName => dependencyCache.Add(dependencyName, singleton));

            return singleton;
        }

        /// <summary>
        /// Start singletons that are startable.
        /// </summary>
        public void Start()
        {
            Singletons.ForType((IStartable s) => {
                logger.LogInfo("Starting singleton: " + s.GetType().Name);

                try
                {
                    s.Start();
                }
                catch (Exception ex)
                {
                    logger.LogError("Exception thrown on startup of singleton: " + s.GetType().Name, ex);
                }
            });
        }

        /// <summary>
        /// Shutdown started singletons.
        /// </summary>
        public void Shutdown()
        {
            Singletons.ForType((IStartable s) => {
                logger.LogInfo("Stopping singleton: " + s.GetType().Name);

                try
                {
                    s.Shutdown();
                }
                catch (Exception ex)
                {
                    logger.LogError("Exception thrown on shutdown of singleton: " + s.GetType().Name, ex);
                }
            });
        }
    }
}
