using Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utils.Dbg;

namespace Utils
{
    /// <summary>
    /// Manages singletons.
    /// </summary>
    public class SingletonManager
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
        /// Find a dependency to a singleton by name.
        /// Returns null if the singleton doesn't exist.
        /// </summary>
        public object ResolveDependency(string name)
        {
            object singleton;
            if (!dependencyCache.TryGetValue(name, out singleton))
            {
                return null;
            }

            return singleton;
        }

        /// <summary>
        /// Find and instantiate singletons.
        /// </summary>
        public void InitSingletons()
        {
            var singletons = new List<object>();

            var types = reflection.FindTypesMarkedByAttributes(new Type[] { typeof(SingletonAttribute) });
            var orderedTypes = factory.OrderByDeps<SingletonAttribute>(types, a => a.DependencyName);

            logger.LogInfo("Found singletons: " + orderedTypes.Select(t => t.Name).Join(", "));

            foreach (var type in orderedTypes)
            {
                logger.LogInfo("Creating singleton: " + type.Name);

                // Instantiate the singleton.
                var singleton = factory.Create(type);

                var attributes = reflection.GetAttributes<SingletonAttribute>(type);
                foreach (var attribute in attributes)
                {
                    // Add the singleton as a dependency.
                    if (attribute.DependencyName != null)
                    {
                        dependencyCache.Add(attribute.DependencyName, singleton);
                    }
                }

                singletons.Add(singleton);
            }

            Singletons = singletons.ToArray();
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
