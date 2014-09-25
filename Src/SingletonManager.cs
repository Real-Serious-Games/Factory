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
        /// Register a singleton with the singleton manager.
        /// The singleton will be instantiated when InstantiateSingletons is called.
        /// </summary>
        void RegisterSingleton(SingletonDef singletonDef);

        /// <summary>
        /// Find a dependency to a singleton by name.
        /// Returns null if the singleton doesn't exist.
        /// </summary>
        object ResolveDependency(string dependencyName, IFactory factory);
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

        public SingletonManager(IReflection reflection, ILogger logger)
        {
            Argument.NotNull(() => reflection);
            Argument.NotNull(() => logger);

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
        public object ResolveDependency(string dependencyName, IFactory factory)
        {
            Argument.StringNotNullOrEmpty(() => dependencyName);
            Argument.NotNull(() => factory);

            object singleton;
            if (!dependencyCache.TryGetValue(dependencyName, out singleton))
            {
                // See if we can lazy init the singleton.
                var lazySingletonDef = singletonDefs
                    .Where(singletonDef => singletonDef.lazy)
                    .Where(singletonDef => singletonDef.dependencyNames.Contains(dependencyName))
                    .FirstOrDefault();
                if (lazySingletonDef != null)
                {
                    return InstantiateSingleton(lazySingletonDef, factory);
                }
                return null;
            }

            return singleton;
        }

        /// <summary>
        /// Instantiate all known (non-lazy) singletons.
        /// </summary>
        public void InstantiateSingletons(IFactory factory)
        {
            Argument.NotNull(() => factory);

            Singletons = singletonDefs
                .Where(singletonDef => !singletonDef.lazy)
                .Select(singletonDef => InstantiateSingleton(singletonDef, factory))
                .ToArray();
        }

        /// <summary>
        /// Instantiate a singleton from a definition.
        /// </summary>
        private object InstantiateSingleton(SingletonDef singletonDef, IFactory factory)
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

        /// <summary>
        /// Get the factory names for the specified type.
        /// </summary>
        public static IEnumerable<string> GetFactoryNames<AttributeT>(Type type, Func<AttributeT, string> typeNameSelector)
            where AttributeT : Attribute
        {
            return ReflectionUtils
                .GetAttributes<AttributeT>(type)
                .Select(a => typeNameSelector(a))
                .Where(n => !string.IsNullOrEmpty(n));
        }

        /// <summary>
        /// Order specified types by their dependencies.
        /// </summary>
        public static IEnumerable<Type> OrderByDeps<AttributeT>(IEnumerable<Type> types, Func<AttributeT, string> typeNameSelector, IFactory factory)
            where AttributeT : Attribute
        {
            var dependencyMap = types
                .SelectMany(t =>
                {
                    return GetFactoryNames(t, typeNameSelector)
                        .Select(n => new { Type = t, TypeName = n });
                })
                .ToDictionary(i => i.TypeName, i => i.Type);

            var typesRemaining = new Queue<Type>(types);
            var dependenciesSatisfied = new HashSet<string>();
            var output = new List<Type>();
            var typesAlreadySeen = new HashSet<Type>();

            while (typesRemaining.Count > 0)
            {
                var type = typesRemaining.Dequeue();

                var allDepsSatisfied =
                    DetermineDeps(type, factory)
                    .Where(n => dependencyMap.ContainsKey(n))
                    .All(n => dependenciesSatisfied.Contains(n));

                if (allDepsSatisfied)
                {
                    output.Add(type);

                    ReflectionUtils.GetAttributes<AttributeT>(type)
                        .Select(a => typeNameSelector(a))
                        .Where(n => !string.IsNullOrEmpty(n))
                        .Each(n => dependenciesSatisfied.Add(n));
                }
                else
                {
                    if (typesAlreadySeen.Contains(type))
                    {
                        throw new ApplicationException("Already seen type: " + type.Name + ", it is possible there is a circular dependency between types.");
                    }

                    // Record the types we have already attempted to process to make sure we are not 
                    // iterating forever on a circular dependency.
                    typesAlreadySeen.Add(type);

                    typesRemaining.Enqueue(type);
                }
            }

            return output.ToArray();
        }

        /// <summary>
        /// Find the requested type and its dependencies.
        /// </summary>
        private static IEnumerable<string> FindDeps(string typeName, IFactory factory)
        {
            yield return typeName;

            var registeredType = factory.FindType(typeName);
            if (registeredType != null)
            {
                // Merge in sub-dependencies.
                foreach (var subDep in DetermineDeps(registeredType, factory))
                {
                    yield return subDep;
                }
            }
        }

        /// <summary>
        /// Determine the dependency tree for a particular type.
        /// </summary>
        private static IEnumerable<string> DetermineDeps(Type type, IFactory factory)
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
                .Distinct()
                .SelectMany(n => FindDeps(n, factory));
        }
    }
}
