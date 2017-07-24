using RSG.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace RSG
{
    /// <summary>
    /// Inteface to an object factory.  Indirectly create objects.  Creation of objects is seperated from the code that uses those objects.
    /// </summary>
    public interface IFactory
    {
        /// <summary>
        /// Name of the factory, useful for debugging.
        /// </summary>
        string Name { get; } 

        /// <summary>
        /// Create an instance of the class that implements the specifid type.
        /// </summary>
        T CreateView<T>(Type type, params object[] args);

        /// <summary>
        /// Create an instance of the a named type.
        /// </summary>
        T Create<T>(string typeName, params object[] args);

        /// <summary>
        /// Create an instance of the a named type.
        /// </summary>
        T CreateInterface<T>(params object[] args);

        /// <summary>
        /// Create an object from its type.
        /// </summary>
        object Create(Type type, params object[] args);

        /// <summary>
        /// Add a factory creatable defined by its interface.
        /// </summary>
        IFactory Type<InterfaceT>(Type type);

        /// <summary>
        /// Add a factory creatable defined by its name.
        /// </summary>
        IFactory Type(string typeName, Type type);

        /// <summary>
        /// Find the type of the factory creatable that implements the specified interface.
        /// Returns null if none was found.
        /// </summary>
        Type FindType<InterfaceT>();

        /// <summary>
        /// Find the type of a factory creatable represented by the specified name.
        /// Returns null if none was found.
        /// </summary>
        Type FindType(string typeName);

        /// <summary>
        /// Create a factory override, where the dependencies can be modified.
        /// </summary>
        IFactory Override();

        /// <summary>
        /// Create a factory override, where the dependencies can be modified.
        /// </summary>
        IFactory Override(string name);

        /// <summary>
        /// Register a typed dependency with the factory.
        /// </summary>
        IFactory Dep<DepT>(DepT instance);

        /// <summary>
        /// Register a typed dependency with the factory.
        /// </summary>
        IFactory Dep(Type interfaceType, object dependency);

        /// <summary>
        /// Register a named dependency with the factory.
        /// </summary>
        IFactory Dep(string dependencyName, object dependency);

        /// <summary>
        /// Register a provider that can fulfil a dependency.
        /// </summary>
        IFactory AddDependencyProvider(IDependencyProvider provider);

        /// <summary>
        /// Remove a typed dependency from the factory.
        /// </summary>
        IFactory RemoveDep<DepT>();

        /// <summary>
        /// Remove a typed dependency from the factory.
        /// </summary>
        IFactory RemoveDep(Type dependencyType);

        /// <summary>
        /// Remove a named dependency from the factory.
        /// </summary>
        IFactory RemoveDep(string dependencyName);

        /// <summary>
        /// Retreive a typed dependency.
        /// </summary>
        DepT RetreiveDependency<DepT>();

        /// <summary>
        /// Retreive a named dependency.
        /// </summary>
        object RetreiveDependency(string name);

        /// <summary>
        /// Returns true if the requested dependency is registered with the factory.
        /// </summary>
        bool IsDependencyRegistered<InterfaceT>();

        /// <summary>
        /// Returns true if the requested dependency is registered with the factory.
        /// </summary>
        bool IsDependencyRegistered(string name);

        /// <summary>
        /// Resolve and inject dependencies.
        /// </summary>
        void ResolveDependencies(object instance);

        /// <summary>
        /// Resolve a dependency. Creates it or returns the cached version.
        /// </summary>
        T ResolveDep<T>();

        /// <summary>
        /// Resolve a dependency. Creates it or returns the cached version.
        /// </summary>
        object ResolveDep(string dependencyName);

        /// <summary>
        /// Version of resolve dependency that can do checking for circular dependencies.
        /// </summary>
        object ResolveDep(string dependencyName, Stack<Type> dependencyChain);

        /// <summary>
        /// Resolve a dependency by quering plugins.
        /// Returns null if dependency was not found.
        /// </summary>
        object ResolveDependency(string dependencyName);
    }

    /// <summary>
    /// Object factory.  Indirectly create objects.  Creation of objects is seperated from the code that uses those objects.
    /// </summary>
    public class Factory : IFactory
    {
        private static readonly object[] emptyArray = new object[0];

        /// <summary>
        /// Fallback if this factory can't create the required object.
        /// </summary>
        IFactory fallbackFactory;

        /// <summary>
        /// Maps names to types.
        /// </summary>
        private Dictionary<string, Type> typeMap = new Dictionary<string, Type>();

        /// <summary>
        /// Use for logging.
        /// </summary>
        private ILogger logger;

        /// <summary>
        /// Provides reflection services to the factory.
        /// </summary>
        private IReflection reflection;

        /// <summary>
        /// Dictionary that contains pre-instantiated dependencies.
        /// </summary>
        private Dictionary<string, object> dependencies = new Dictionary<string, object>();

        /// <summary>
        /// List of plugins that are can provide dependencies when they are otherwise not satisfied within the factory.
        /// </summary>
        private List<IDependencyProvider> dependencyProviders = new List<IDependencyProvider>();

        /// <summary>
        /// Name of the factory, useful for debugging.
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        public Factory(string name, ILogger logger)
        {
            Argument.StringNotNullOrEmpty(() => name);
            Argument.NotNull(() => logger);

            this.Name = name;
            this.logger = logger;
            this.reflection = new Reflection();

            this.Dep<IFactory>(this);
        }

        public Factory(string name, ILogger logger, IReflection reflection)
        {
            Argument.StringNotNullOrEmpty(() => name);
            Argument.NotNull(() => logger);
            Argument.NotNull(() => reflection);

            this.Name = name;
            this.logger = logger;
            this.reflection = reflection;

            this.Dep<IFactory>(this);
        }

        public Factory(string name, IFactory fallbackFactory, ILogger logger)
        {
            Argument.StringNotNullOrEmpty(() => name);
            Argument.NotNull(() => fallbackFactory);
            Argument.NotNull(() => logger);

            this.Name = name;
            this.fallbackFactory = fallbackFactory;
            this.logger = logger;
            this.reflection = new Reflection();

            this.Dep<IFactory>(this);
        }

        public Factory(string name, IFactory fallbackFactory, ILogger logger, IReflection reflection)
        {
            Argument.StringNotNullOrEmpty(() => name);
            Argument.NotNull(() => fallbackFactory);
            Argument.NotNull(() => logger);
            Argument.NotNull(() => reflection);
                        
            this.Name = name;
            this.fallbackFactory = fallbackFactory;
            this.logger = logger;
            this.reflection = reflection;

            this.Dep<IFactory>(this);
        }

        /// <summary>
        /// Helper function to flatten out a list of types and base interfaces for
        /// use by the ResolveView fn.
        /// </summary>
        private IEnumerable<Type> FlattenInterfaces(Type type)
        {
            yield return type;

            foreach (var interfaceType in type.GetInterfaces())
            {
                yield return interfaceType;
            }
        }

        /// <summary>
        /// Create an instance of the class that implements the specifid type.
        /// </summary>
        public T CreateView<T>(Type dataType, params object[] args)
        {
            var viewTypeName = typeof(T).Name;

            logger.LogVerbose("[Factory: " + Name + "]: Resolving view " + viewTypeName + " for data " + dataType.Name);

            foreach (var typeToCheck in FlattenInterfaces(dataType))
            {
                var viewName = viewTypeName + "_" + typeToCheck.Name;
                if (FindType(viewName) != null)
                {
                    return InternalCreate<T>(viewName, args);
                }
            }

            throw new ApplicationException("Failed to create type with interface " + viewTypeName + " based on data type " + dataType.Name);
        }

        /// <summary>
        /// Create an instance of the a named type.
        /// </summary>
        public T Create<T>(string typeName, params object[] args)
        {
            logger.LogVerbose("[Factory: " + Name + "]: Creating " + typeName);

            return InternalCreate<T>(typeName, args);
        }

        /// <summary>
        /// Get the name of the requested type.
        /// </summary>
        public static string GetTypeName(Type type)
        {
            var genericArgs = type.GetGenericArguments();
            if (genericArgs.Any())
            {
                var typeNames = genericArgs
                    .Select(t => GetTypeName(t))
                    .Join(',');
                return type.Name + "<" + typeNames + ">";
            }
            else
            {
                return type.Name;
            }
        }

        /// <summary>
        /// Create an instance of a type from its interface.
        /// </summary>
        public T CreateInterface<T>(params object[] args)
        {
            var interfaceName = GetTypeName(typeof(T));

            logger.LogVerbose("[Factory: " + Name + "]: Resolving interface " + interfaceName);

            return InternalCreate<T>(interfaceName, args);
        }

        /// <summary>
        /// Create an object from its type.
        /// </summary>
        public object Create(Type type, params object[] args)
        {
            Argument.NotNull(() => type);

            return Instantiate<object>(type, args, new Stack<Type>());
        }

        /// <summary>
        /// Create an instance of the a named type.
        /// </summary>
        private T InternalCreate<T>(string typeName, object[] args)
        {
            Argument.StringNotNullOrEmpty(() => typeName);

            var type = FindType(typeName);
            if (type == null)
            {
                throw new ApplicationException("Request to create unknown type: " + typeName);
            }

            return Instantiate<T>(type, args, new Stack<Type>());
        }

        /// <summary>
        /// Add a factory creatable defined by its interface.
        /// </summary>
        public IFactory Type<InterfaceT>(Type type)
        {
            return Type(typeof(InterfaceT).Name, type);
        }

        /// <summary>
        /// Add a factory creatable defined by its name.
        /// </summary>
        public IFactory Type(string typeName, Type type)
        {
            Argument.StringNotNullOrEmpty(() => typeName);
            Argument.NotNull(() => type);

            Type existingType;
            if (typeMap.TryGetValue(typeName, out existingType))
            {
                throw new ApplicationException("A type has already been registered for name '" + typeName + "'. Attempting to register type " + type.FullName + ", but type " + existingType.FullName + " is already registered with that name.");
            }

            typeMap.Add(typeName, type);

            return this;
        }

        /// <summary>
        /// Find the type of the factory creatable that implements the specified interface.
        /// Returns null if none was found.
        /// </summary>
        public Type FindType<InterfaceT>()
        {
            return FindType(typeof(InterfaceT).Name);
        }

        /// <summary>
        /// Find the type of a factory creatable represented by the specified name.
        /// Returns null if none was found.
        /// </summary>
        public Type FindType(string typeName)
        {
            Argument.StringNotNullOrEmpty(() => typeName);

            Type type;
            if (typeMap.TryGetValue(typeName, out type))
            {
                logger.LogVerbose("[Factory: " + Name + "]: Satisfied request for " + typeName + " from factory " + Name);

                return type;
            }
            else
            {
                // Query providers to find dependency type.
                var dependencyType = dependencyProviders
                    .Select(provider => provider.FindDependencyType(typeName))
                    .Where(dep => dep != null)
                    .WhereMultiple((dependencies) =>
                    {
                        var msg =
                            "Attempting to resolve a dependency via the dependency provider, but multiple dependencies have been found that match " + typeName + "\n" +
                            "The following matches were found: \n" +
                            dependencies.Select(dep => "\t" + dep.GetType().Name).Join("\n");

                        throw new ApplicationException(msg);
                    })
                    .FirstOrDefault();

                if (dependencyType != null)
                {
                    // Plugin dependency resolved at this level.
                    return dependencyType;
                }
                else if (fallbackFactory != null)
                {
                    return fallbackFactory.FindType(typeName);
                }

                return null;
            }
        }

        /// <summary>
        /// Instantiate the specified type with requested args.
        /// </summary>
        private T Instantiate<T>(Type type, object[] args, Stack<Type> dependencyChain)
        {
            if (dependencyChain.Contains(type))
            {
                // Circular dependency detected.
                var depChain = dependencyChain.AsEnumerable().Select(t => t.Name).Join(" -> ");
                throw new ApplicationException("Circular dependency detected: " + depChain);
            }

            // Record the dependency chaining to check for circular dependencies.
            dependencyChain.Push(type);

            try
            {
                var argTypes = args.Select(a => a == null ? null : a.GetType()).ToArray();
                var constructor = ResolveConstructor(type, argTypes);
                var parameters = constructor.GetParameters();
                int numExpectedArgs = parameters.Length;
                int numDependenciesToResolve = numExpectedArgs - args.Length;
                if (numDependenciesToResolve > 0)
                {
                    // Resolve dependencies any remaining parameters.
                    var remainingParameterTypes =
                        parameters
                            .Skip(args.Length)
                            .Select(p => p.ParameterType)
                            .ToArray();

                    if (remainingParameterTypes.Count() > 0)
                    {
                        logger.LogVerbose("Resolving constructor parameter dependencies:");

                        var dependencyArgs =
                            remainingParameterTypes
                                .Select(t => ResolveDep(t.Name, dependencyChain))
                                .ToArray();

                        args = args.Concat(dependencyArgs).ToArray();
                    }
                }

                T instance;

                logger.LogVerbose("Invoking constructor: " + constructor);

                instance = (T)constructor.Invoke(args);

                logger.LogVerbose("Resolving properties:");

                ResolveDependencies(instance, dependencyChain);

                return instance;
            }
            finally
            {
                dependencyChain.Pop();
            }
        }

        /// <summary>
        /// Create a factory override, where the dependencies can be modified.
        /// </summary>
        public IFactory Override()
        {
            return Override("Override of " + Name);
        }

        /// <summary>
        /// Create a factory override, where the dependencies can be modified.
        /// </summary>
        public IFactory Override(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Invalid factory name.", "name");
            }

            return new Factory(name, this, logger, reflection);
        }

        /// <summary>
        /// Register a typed dependency with the factory.
        /// </summary>
        public IFactory Dep<DepT>(DepT dependency)
        {
            Argument.NotNull(() => dependency);

            return Dep(GetTypeName(typeof(DepT)), dependency);
        }

        /// <summary>
        /// Register a typed dependency with the factory.
        /// </summary>
        public IFactory Dep(Type dependencyType, object dependency)
        {
            Argument.NotNull(() => dependencyType);
            Argument.NotNull(() => dependency);

            return Dep(GetTypeName(dependencyType), dependency);
        }

        /// <summary>
        /// Register a named dependency with the factory.
        /// </summary>
        public IFactory Dep(string dependencyName, object dependency)
        {
            Argument.StringNotNullOrEmpty(() => dependencyName);
            Argument.NotNull(() => dependency);

            dependencies.Add(dependencyName, dependency);
            return this;
        }

        /// <summary>
        /// Remove a typed dependency from the factory.
        /// </summary>
        public IFactory RemoveDep<DepT>()
        {
            return RemoveDep(typeof(DepT));
        }

        /// <summary>
        /// Remove a typed dependency from the factory.
        /// </summary>
        public IFactory RemoveDep(Type dependencyType)
        {
            Argument.NotNull(() => dependencyType);

            return RemoveDep(GetTypeName(dependencyType));
        }

        /// <summary>
        /// Remove a named dependency from the factory.
        /// </summary>
        public IFactory RemoveDep(string dependencyName)
        {
            Argument.StringNotNullOrEmpty(() => dependencyName);

            dependencies.Remove(dependencyName);
            return this;
        }

        /// <summary>
        /// Register a provider that can fulfil a dependency.
        /// </summary>
        public IFactory AddDependencyProvider(IDependencyProvider provider)
        {
            dependencyProviders.Add(provider);
            return this;
        }

        /// <summary>
        /// Resolve the particular contructor to call.
        /// </summary>
        public ConstructorInfo ResolveConstructor(Type type, Type[] argTypes)
        {
            Argument.NotNull(() => type);
            Argument.NotNull(() => argTypes);

            var constructors = 
                type.GetConstructors()
                    // Filter out constructors that don't take at least the args that were passed in.
                    .Where(c => c.GetParameters().Length >= argTypes.Length)
                    // Filter out constructors with arguments that don't match.
                    .Where(c => ArgumentsMatch(c.GetParameters(), argTypes))
                    // Filter out constructors whose remaining arguments don't match availaible dependencies.
                    .Where(c => HaveMatchingDeps(c.GetParameters().Skip(argTypes.Length)))
                    .ToArray();

            if (constructors.Length == 0)
            {
                var msg = "Failed to find a matching constructor for type: " + type.Name;
                ThrowConstructorNotMatched(msg, argTypes, type);
            }
            else if (constructors.Length > 1)
            {
                var msg = "Found multiple matching constructors for type: " + type.Name;
                ThrowConstructorNotMatched(msg, argTypes, type);
            }

            return constructors[0];
        }

        /// <summary>
        /// Throw an exception when an appropriate constructor could not be found for a type.
        /// </summary>
        private void ThrowConstructorNotMatched(string msg, Type[] argTypes, Type type)
        {
            msg += "\nSpecified Arguments:\n";

            if (argTypes.Length == 0)
            {
                msg += "  None\n";
            }
            else
            {
                foreach (var argType in argTypes)
                {
                    msg += "  " + argType.Name + "\n";
                }
            }

            msg += "Available Constructors:\n";

            foreach (var constructor in type.GetConstructors())
            {
                msg += "  " + constructor.ToString() + "\n";

                var dependencyInjectedParameters = constructor.GetParameters().Skip(argTypes.Length);
                var argumentNames = DetermineArgumentsThatCanBeInjected(dependencyInjectedParameters);
                if (argumentNames.Length > 0)
                {
                    msg += "    The following arguments can be injected: \n";
                    msg += argumentNames.Select(name => "      " + name).Join('\n');
                }
            }

            logger.LogError(msg);

            throw new ApplicationException(msg);
        }

        /// <summary>
        /// Return true if the parameters match the specified argument types.
        /// </summary>
        private bool ArgumentsMatch(ParameterInfo[] parameters, Type[] argTypes)
        {
            for (var i = 0; i < argTypes.Length; ++i)
            {
                var argType = argTypes[i];
                var paramType = parameters[i].ParameterType;

                if (argType == null)
                {
                    // If argument is null, but the parameter is not an interface or class, fail the match.
                    if (!paramType.IsInterface && !paramType.IsClass)
                    {
                        return false;
                    }
                }
                else if (!paramType.IsAssignableFrom(argType))
                {
                    // If the parameter isn't assignmable from the argument, fail the match.
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns true if the specified parameters have dependencies available for injection.
        /// </summary>
        private bool HaveMatchingDeps(IEnumerable<ParameterInfo> parameters)
        {
            foreach (var parameter in parameters)
            {
                var type = parameter.ParameterType;

                if (!type.IsInterface)
                {
                    return false; // Only consider interfaces for dep injection.
                }

                if (!IsDependencyRegistered(GetTypeName(type)))
                {
                    if (FindType(GetTypeName(type)) == null)
                    {
                        return false; // Can't satisfy dependency from the dependency provider or the factory.
                    }                    
                }
            }

            return true;
        }

        /// <summary>
        /// Returns a list of arguments that can be matched by dependency injection.
        /// </summary>
        private string[] DetermineArgumentsThatCanBeInjected(IEnumerable<ParameterInfo> parameters)
        {
            return
                parameters
                    .Where(parameter =>
                    {
                        var name = GetTypeName(parameter.ParameterType);
                        return IsDependencyRegistered(name) || FindType(name) != null;
                    })
                    .Select(parameter =>
                    {
                        var name = GetTypeName(parameter.ParameterType);
                        if (IsDependencyRegistered(name))
                        {
                            return parameter.Name + " (dependency: " + RetreiveDependency(name).GetType().Name + ")";
                        }
                        else
                        {
                            return parameter.Name + " (factory creatable: " + FindType(name).Name + ")";
                        }
                    })
                    .ToArray();
        }

        /// <summary>
        /// Retreive a typed dependency.
        /// </summary>
        public DepT RetreiveDependency<DepT>()
        {
            return (DepT) RetreiveDependency(typeof (DepT).Name);
        }

        /// <summary>
        /// Retreive a named dependency.
        /// </summary>
        public object RetreiveDependency(string name)
        {
            Argument.StringNotNullOrEmpty(() => name);

            object dependency;
            if (dependencies.TryGetValue(name, out dependency))
            {
                logger.LogVerbose("[DependencyProvider: " + Name + "]: Satisfied dependency " + name + " as " + dependency.GetType().Name + " from " + this.Name);

                return dependency;
            }
            else if (fallbackFactory != null)
            {
                return fallbackFactory.RetreiveDependency(name);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Returns true if the requested dependency is registered with the factory.
        /// </summary>
        public bool IsDependencyRegistered<InterfaceT>()
        {
            return IsDependencyRegistered(typeof(InterfaceT).Name);
        }

        /// <summary>
        /// Returns true if the requested dependency is registered with the factory.
        /// </summary>
        public bool IsDependencyRegistered(string name)
        {
            Argument.StringNotNullOrEmpty(() => name);

            if (dependencies.ContainsKey(name))
            {
                return true;
            }
            else if (fallbackFactory != null)
            {
                return fallbackFactory.IsDependencyRegistered(name);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Resolve and inject dependencies.
        /// </summary>
        public void ResolveDependencies(object instance)
        {
            ResolveDependencies(instance, new Stack<Type>());   
        }

        /// <summary>
        /// Internal version that can check for circular dependencies.
        /// </summary>
        private void ResolveDependencies(object instance, Stack<Type> dependencyChain)
        {
            Argument.NotNull(() => instance);

            logger.LogVerbose("[Injector]: Resolving dependencies for instance of " + instance.GetType().Name);

            if (instance == null)
            {
                throw new ArgumentNullException("instance", "Can't resolve dependencies for a null object!");
            }

            var type = instance.GetType();

            logger.LogVerbose("Resolving property dependencies for " + type.Name);

            foreach (var property in type.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var dependencyAttribute = ReflectionUtils.GetPropertyAttribute<DependencyAttribute>(property);
                if (dependencyAttribute != null)
                {
                    throw new ApplicationException("Non public property " + property.Name + " should not have [Dependency] attribute!");
                }
            }

            foreach (var property in type.GetProperties())
            {
                var dependencyAttribute = ReflectionUtils.GetPropertyAttribute<DependencyAttribute>(property);
                if (dependencyAttribute != null)
                {
                    var dependencyType = dependencyAttribute.DependencyType != null ? dependencyAttribute.DependencyType : property.PropertyType;

                    logger.LogVerbose(property.Name + " => " + dependencyType.Name);

                    // Create and assign the dependency.
                    ResolvePropertyDep(instance, property, dependencyType, dependencyChain);
                }
            }
        }

        /// <summary>
        /// Resolve the dependency for a particular property.
        /// </summary>
        private void ResolvePropertyDep(object instance, PropertyInfo property, Type dependencyType, Stack<Type> dependencyChain)
        {
            Argument.NotNull(() => instance);
            Argument.NotNull(() => property);
            Argument.NotNull(() => dependencyType);

            if (property.GetValue(instance, null) != null)
            {
                //
                // Dependency already set, don't resolve it again.
                //
                return;
            }

            var dependency = ResolveDep(GetTypeName(dependencyType), dependencyChain);
            property.SetValue(instance, dependency, null);
        }

        /// <summary>
        /// Resolve a dependency. Creates it or returns the cached version.
        /// </summary>
        public T ResolveDep<T>()
        {
            return (T)ResolveDep(GetTypeName(typeof(T)));
        }

        /// <summary>
        /// Resolve a dependency. Creates it or returns the cached version.
        /// </summary>
        public object ResolveDep(string dependencyName)
        {
            Argument.StringNotNullOrEmpty(() => dependencyName);

            return ResolveDep(dependencyName, new Stack<Type>());
        }

        /// <summary>
        /// Version of resolve dependency that can do checking for circular dependencies.
        /// </summary>
        public object ResolveDep(string dependencyName, Stack<Type> dependencyChain)
        {
            logger.LogVerbose("[Injector]: Resolving dependency " + dependencyName);

            var dependency = RetreiveDependency(dependencyName);
            if (dependency != null)
            {
                // Return cached dependency.
                //
                return dependency;
            }

            Type dependencyType;
            if (typeMap.TryGetValue(dependencyName, out dependencyType))
            {
                return Instantiate<object>(dependencyType, emptyArray, dependencyChain);
            }

            // Query providers to provide the dependency.
            var pluginDependency = ResolveDependency(dependencyName);
            if (pluginDependency != null)
            {
                return pluginDependency;
            }
                
            if (fallbackFactory != null)
            {
                dependencyType = fallbackFactory.FindType(dependencyName);
                if (dependencyType != null)
                {
                    return Instantiate<object>(dependencyType, emptyArray, dependencyChain);
                }                    
            }


            throw new ApplicationException("Failed to find or create dependency " + dependencyName);
        }

        /// <summary>
        /// Resolve a dependency by quering plugins.
        /// Returns null if dependency was not found.
        /// </summary>
        public object ResolveDependency(string dependencyName)
        {
            Argument.StringNotNullOrEmpty(() => dependencyName);

            // Query plugins to provide the dependency.
            var pluginDependency = dependencyProviders
                .Select(provider => provider.ResolveDependency(dependencyName))
                .Where(dep => dep != null)
                .WhereMultiple((dependencies) =>
                {
                    var msg = 
                        "Attempting to resolve a dependency via the dependency provider, but multiple dependencies have been found that match " + dependencyName + "\n" +
                        "The following matches were found: \n" +
                        dependencies.Select(dep => "\t" + dep.GetType().Name).Join("\n");

                    throw new ApplicationException(msg);                        
                })
                .FirstOrDefault();
            if (pluginDependency != null)
            {
                // Plugin dependency resolved at this level.
                return pluginDependency;
            }
            else if (fallbackFactory != null)
            {
                return fallbackFactory.ResolveDependency(dependencyName);
            }

            return null;
        }

        /// <summary>
        /// Automatically register types and dependencies with the factory.
        /// </summary>
        public void AutoRegisterTypes()
        {
            Argument.NotNull(() => reflection);

            AutoRegisterTypes(reflection, this);
        }

        /// <summary>
        /// Helper function to search for and auto-register factory types.
        /// Mockable interfaces are passed in for testing.
        /// </summary>
        public static void AutoRegisterTypes(IReflection reflection, IFactory factory)
        {
            Argument.NotNull(() => reflection);
            Argument.NotNull(() => factory);

            // Add factory creatable definitions
            var factoryCreatableTypes = reflection.FindTypesMarkedByAttributes(new Type[] { typeof(FactoryCreatableAttribute) });
            foreach (var factoryCreatableType in factoryCreatableTypes)
            {
                var attributes = reflection.GetAttributes<FactoryCreatableAttribute>(factoryCreatableType);
                foreach (var attribute in attributes)
                {
                    var name = attribute.TypeName;
                    if (string.IsNullOrEmpty(name))
                    {
                        name = factoryCreatableType.Name;
                    }

                    factory.Type(name, factoryCreatableType);
                }
            }
        }

        /// <summary>
        /// Automatically find and instantiate singletons, classes that are marked with the [Singleton] attribute, or an attribute
        /// that derives from SingletonAttribute.
        /// </summary>
        public ISingletonManager AutoInstantiateSingletons()
        {
            var singletonManager = new SingletonManager(logger, this);
            var singletonScanner = new SingletonScanner(reflection, logger, singletonManager);

            // Connect the singleton manager to the factory so that it can be used to satisify dependencies.
            AddDependencyProvider(singletonManager);

            // Auto register singletons whose types are marked with the Singleton attribute.
            singletonScanner.ScanSingletonTypes();

            // Instantiate singletons.
            singletonManager.InstantiateSingletons(this);

            return singletonManager;
        }
    }
}
