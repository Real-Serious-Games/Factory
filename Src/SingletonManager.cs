using RSG.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace RSG
{
    /// <summary>
    /// Defines a singleton.
    /// </summary>
    public class SingletonDef
    {
        /// <summary>
        /// The type of the singleton.
        /// </summary>
        public Type singletonType;

        /// <summary>
        /// Names of dependencies that the singleton satisfies.
        /// </summary>
        public string[] dependencyNames;

        /// <summary>
        /// Set to true to make the singleton lazy. It will only be instantiated when resolved the first time.
        /// </summary>
        public bool lazy;

        /// <summary>
        /// Custom instantiation.
        /// </summary>
        public Func<IFactory, Type, object> Instantiate;

        public override string ToString()
        {
            return singletonType.Name + " (" + (lazy ? "lazy" : "normal") + ") [" + dependencyNames.Join(",") + "]";
        }
    }

    /// <summary>
    /// Interface to the singleton manager (for mocking).
    /// </summary>
    public interface ISingletonManager
    {
        /// <summary>
        /// Singletons that are instantiated.
        /// </summary>
        object[] Singletons { get; }

        /// <summary>
        /// Register a singleton with the singleton manager.
        /// The singleton will be instantiated when InstantiateSingletons is called.
        /// </summary>
        void RegisterSingleton(SingletonDef singletonDef);

        /// <summary>
        /// Instantiate all known (non-lazy) singletons.
        /// </summary>
        void InstantiateSingletons(IFactory factory);

        /// <summary>
        /// Start singletons that are startable.
        /// </summary>
        void Startup();

        /// <summary>
        /// Shutdown started singletons.
        /// </summary>
        void Shutdown();
    }

    /// <summary>
    /// Manages singletons.
    /// </summary>
    public class SingletonManager : ISingletonManager, IDependencyProvider
    {
        /// <summary>
        /// Singletons that are instantiated.
        /// </summary>
        public object[] Singletons { get; private set; }

        /// <summary>
        /// Factory used to instantiate singletons.
        /// </summary>
        private readonly IFactory factory;

        /// <summary>
        /// Map that allows singletons to be dependency injected.
        /// Maps 'dependency name' to singleton object.
        /// </summary>
        private readonly Dictionary<string, object> dependencyCache = new Dictionary<string, object>();

        /// <summary>
        /// For logging.
        /// </summary>
        private readonly ILogger logger;

        /// <summary>
        /// Definitions for all known (non-lazy) singletons.
        /// </summary>
        private readonly List<SingletonDef> singletonDefs = new List<SingletonDef>();

        [Obsolete("There is no need to pass reflection into constructor any more")]
        public SingletonManager(IReflection reflection, ILogger logger, IFactory factory)
        {
            Argument.NotNull(() => logger);
            Argument.NotNull(() => factory);

            this.logger = logger;
            this.factory = factory;
            this.Singletons = new object[0];
        }

        public SingletonManager(ILogger logger, IFactory factory)
        {
            Argument.NotNull(() => logger);
            Argument.NotNull(() => factory);

            this.logger = logger;
            this.factory = factory;
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
        /// Resolve a singleton that matches the requested dependency name.
        /// Returns null if none was found.
        /// </summary>
        public object ResolveDependency(string dependencyName)
        {
            Argument.StringNotNullOrEmpty(() => dependencyName);
            Argument.NotNull(() => factory);

            object singleton;
            if (!dependencyCache.TryGetValue(dependencyName, out singleton))
            {
                // See if we can lazy init the singleton.
                var lazySingletonDef = singletonDefs
                    .FirstOrDefault(singletonDef => singletonDef.lazy 
                        && singletonDef.dependencyNames.Contains(dependencyName));

                return lazySingletonDef != null ? InstantiateSingleton(lazySingletonDef, factory) : null;
            }

            return singleton;
        }

        /// <summary>
        /// Find the type of a singleton that matches the requested dependency name.
        /// Returns null if none was found.
        /// </summary>
        public Type FindDependencyType(string dependencyName)
        {
            Argument.StringNotNullOrEmpty(() => dependencyName);

            return singletonDefs
                .Where(singletonDef => singletonDef.dependencyNames.Contains(dependencyName))
                .WhereMultiple((matchingSingletonDefs) =>
                {
                    var msg =
                        "Multiple singleton definitions match dependency " + dependencyName + "\n" +
                        "Matching singletons:\n" +
                        matchingSingletonDefs.Select(def => "\t" + def.singletonType.Name).Join("\n");

                    throw new ApplicationException(msg);
                })
                .Select(singletonDef => singletonDef.singletonType)
                .FirstOrDefault();
        }

        /// <summary>
        /// Instantiate all known (non-lazy) singletons.
        /// </summary>
        public void InstantiateSingletons(IFactory factory)
        {
            Argument.NotNull(() => factory);

            var nonLazySingletons = singletonDefs.Where(singletonDef => !singletonDef.lazy);
            var sortedByDependency = OrderByDeps(nonLazySingletons, factory, logger);

            Singletons = sortedByDependency
                .Select(singletonDef => InstantiateSingleton(singletonDef, factory))
                .Where(singleton => singleton != null)
                .ToArray();
        }

        /// <summary>
        /// Instantiate a singleton from a definition.
        /// </summary>
        private object InstantiateSingleton(SingletonDef singletonDef, IFactory factory)
        {
            var type = singletonDef.singletonType;
            logger.LogVerbose("Instantiating singleton " + type.Name + " that satisfies dependencies " + singletonDef.dependencyNames.Join(", "));

            try
            {
                // Instantiate the singleton.
                var singleton = singletonDef.Instantiate != null ? singletonDef.Instantiate(factory, type) : factory.Create(type);
                singletonDef.dependencyNames.Each(dependencyName => dependencyCache.Add(dependencyName, singleton));

                return singleton;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to instantiate singleton {SingletonTypeName}", type.Name);
                return null;
            }
        }

        /// <summary>
        /// Start singletons that are startable.
        /// </summary>
        public void Startup()
        {
            Singletons.ForType((IStartable s) => {
                logger.LogVerbose("Starting singleton: " + s.GetType().Name);

                try
                {
                    s.Startup();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Exception thrown on startup of singleton: " + s.GetType().Name);
                }
            });
        }

        /// <summary>
        /// Shutdown started singletons.
        /// </summary>
        public void Shutdown()
        {
            // Shutdown must happen in reverse order to startup so that dependencies are still available during shutdown.
            Singletons.Reverse().ForType((IStartable s) => {
                logger.LogVerbose("Stopping singleton: " + s.GetType().Name);

                try
                {
                    s.Shutdown();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Exception thrown on shutdown of singleton: " + s.GetType().Name);
                }
            });
        }

        /// <summary>
        /// Order specified types by their dependencies.
        /// </summary>
        public static IEnumerable<SingletonDef> OrderByDeps(IEnumerable<SingletonDef> singletonDefs, IFactory factory, ILogger logger)
        {
            Argument.NotNull(() => singletonDefs);
            Argument.NotNull(() => factory);

            logger.LogVerbose("Ordering singletons:");

            singletonDefs
                .SelectMany(singletonDef =>
                {
                    return singletonDef.dependencyNames
                        .Select(dependencyName => new
                        {
                            Type = singletonDef,
                            DependencyName = dependencyName
                        });
                })
                .GroupBy(singletonDef => singletonDef.DependencyName)
                .Each(singletonGroup =>
                {
                    if (singletonGroup.Count() > 1) 
                    {
                        logger.LogError("Multiple singletons defined as the dependency " + singletonGroup.Key);

                        singletonGroup.Each(singletonDef =>
                        {
                            logger.LogVerbose(singletonDef.Type.singletonType.Name + " defined as dependency " + singletonDef.DependencyName + "( from DLL " + singletonDef.Type.singletonType.Assembly.FullName + ")");
                        });
                    }
                });

            var dependencyMap = singletonDefs
                .SelectMany(singletonDef =>
                {
                    return singletonDef.dependencyNames
                        .Select(dependencyName => new 
                        { 
                            Type = singletonDef, 
                            DependencyName = dependencyName 
                        });
                })
                .ToDictionary(i => i.DependencyName, i => i.Type);

            var defsRemaining = new Queue<SingletonDef>(singletonDefs);
            var dependenciesSatisfied = new HashSet<string>();
            var output = new List<SingletonDef>();
            var defsAlreadySeen = new Dictionary<SingletonDef, int>();
            var maxTypesSeen = 5;

            while (defsRemaining.Count > 0)
            {
                var singletonDef = defsRemaining.Dequeue();

                // Get the names of all types that this singleton is dependent on.
                var singletonDependsOn = DetermineDeps(singletonDef.singletonType, factory, new string[0]).ToArray();

                var allDepsSatisfied = singletonDependsOn                                                  
                    .Where(dependencyName => dependencyMap.ContainsKey(dependencyName)) // Only care about dependencies that are satisifed by other singletons.
                    .All(dependencyName => dependenciesSatisfied.Contains(dependencyName));                       // Have we seen all other singletons yet that this singleton is dependent on?

                if (allDepsSatisfied)
                {
                    output.Add(singletonDef);

                    logger.LogVerbose("\t" + singletonDef.singletonType.Name);
                    logger.LogVerbose("\t\tImplements: " + singletonDef.dependencyNames.Join(", "));
                    logger.LogVerbose("\t\tDepends on:");
                    singletonDependsOn.Each(dependencyName => logger.LogVerbose("\t\t\t" + dependencyName));
                    logger.LogVerbose("\t\tDepends on (singletons):");
                    singletonDependsOn
                        .Where(dependencyName => dependencyMap.ContainsKey(dependencyName))
                        .Each(dependencyName => logger.LogVerbose("\t\t\t" + dependencyName + (dependenciesSatisfied.Contains(dependencyName) ? " (satisfied)" : " (unsatisified)")));

                    singletonDef.dependencyNames.Each(dependencyName => dependenciesSatisfied.Add(dependencyName));
                }
                else
                {
                    int timesSeen;
                    if (defsAlreadySeen.TryGetValue(singletonDef, out timesSeen))
                    {
                        if (timesSeen >= maxTypesSeen)
                        {
                            throw new ApplicationException("Already seen type: " + singletonDef.singletonType.Name + ", it is possible there is a circular dependency between types or it might be that a dependency can't be satisfied.");
                        }

                        // Increment the number of times we have seen this type.
                        ++defsAlreadySeen[singletonDef];
                    }
                    else
                    {
                        // Record the types we have already attempted to process to make sure we are not 
                        // iterating forever on a circular dependency.
                        defsAlreadySeen[singletonDef] = 1;
                    }

                    defsRemaining.Enqueue(singletonDef);
                }
            }

            return output.ToArray();
        }

        /// <summary>
        /// Find the requested type and its dependencies.
        /// </summary>
        private static IEnumerable<string> FindDeps(string typeName, IFactory factory, IEnumerable<string> typesConsidered)
        {
            if (typesConsidered.Contains(typeName))
            {
                throw new ApplicationException("Already considered " + typeName + ", could be in a circular dependency. Types considered: " + typesConsidered.Join(", "));
            }

            yield return typeName;            

            var registeredType = factory.FindType(typeName);
            if (registeredType != null)
            {
                // Merge in sub-dependencies.
                foreach (var subDep in DetermineDeps(registeredType, factory, typesConsidered.ConcatItems(typeName)))
                {
                    yield return subDep;
                }
            }
        }

        /// <summary>
        /// Determine the dependency tree for a particular type.
        /// </summary>
        private static IEnumerable<string> DetermineDeps(Type type, IFactory factory, IEnumerable<string> typesConsidered)
        {
            try
            {
                return type    // Start with types of properties.
                    .GetProperties()
                    .Where(p => ReflectionUtils.PropertyHasAttribute<DependencyAttribute>(p))
                    .Select(p => Factory.GetTypeName(p.PropertyType))
                    // Merge in constructor parameter types.
                    .Concat(
                        type
                            .GetConstructors()
                            .SelectMany(c => c.GetParameters())
                            .Select(p => Factory.GetTypeName(p.ParameterType))
                    )
                    .SelectMany(n => FindDeps(n, factory, typesConsidered))
                    .Distinct()
                    .ToArray(); // Force evaluation so that any exceptions are thrown.
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Exception was thrown while determining dependencies for " + type.Name, ex);
            }
        }
    }
}
