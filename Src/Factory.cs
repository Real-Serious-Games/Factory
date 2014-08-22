using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Utils.Dbg;

namespace Utils
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
        /// Returns true if the specified type can be instantiated by the factory
        /// </summary>
        bool HasType<InterfaceT>();

        /// <summary>
        /// Returns true if the specified type can be instantiated by the factory
        /// </summary>
        bool HasType(string typeName);

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
        /// Order specified types by their dependencies.
        /// </summary>
        IEnumerable<Type> OrderByDeps<AttributeT>(IEnumerable<Type> types, Func<AttributeT, string> typeNameSelector)
            where AttributeT : Attribute;
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
        /// The dictionary that contains the dependencies.
        /// </summary>
        private Dictionary<string, object> dependencies = new Dictionary<string, object>();

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
        public T CreateView<T>(Type type, params object[] args)
        {
            var viewTypeName = typeof(T).Name;

            logger.LogVerbose("[Factory: " + Name + "]: Resolving view " + viewTypeName + " for data " + type.Name);
            logger.Indent();

            try
            {
                foreach (var typeToCheck in FlattenInterfaces(type))
                {
                    var viewName = viewTypeName + "_" + typeToCheck.Name;
                    if (HasType(viewName))
                    {
                        return InternalCreate<T>(viewName, args);
                    }
                }

                throw new ApplicationException("Failed to create type with interface " + viewTypeName + " based on data type " + type.Name);
            }
            finally
            {
                logger.Unindent();
            }
        }

        /// <summary>
        /// Create an instance of the a named type.
        /// </summary>
        public T Create<T>(string typeName, params object[] args)
        {
            logger.LogVerbose("[Factory: " + Name + "]: Creating " + typeName);
            logger.Indent();

            var obj = InternalCreate<T>(typeName, args);

            logger.Unindent();

            return obj;
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
            logger.Indent();

            var obj = InternalCreate<T>(interfaceName, args);

            logger.Unindent();

            return obj;
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

            if (typeMap.ContainsKey(typeName))
            {
                throw new ApplicationException("A type has already been registered for name '" + typeName + "'.");
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
            else if (fallbackFactory != null)
            {
                return fallbackFactory.FindType(typeName);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Returns true if the specified type can be instantiated by the factory
        /// </summary>
        public bool HasType<InterfaceT>()
        {
            return HasType(typeof(InterfaceT).Name);
        }

        /// <summary>
        /// Returns true if the specified type can be instantiated by the factory
        /// </summary>
        public bool HasType(string typeName)
        {
            Argument.StringNotNullOrEmpty(() => typeName);

            if (typeMap.ContainsKey(typeName))
            {
                return true;
            }
            else if (fallbackFactory != null)
            {
                return fallbackFactory.HasType(typeName);
            }
            else
            {
                return false;
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
                        logger.Indent();

                        var dependencyArgs =
                            remainingParameterTypes
                                .Select(t => ResolveDep(t.Name, dependencyChain))
                                .ToArray();

                        logger.Unindent();

                        args = args.Concat(dependencyArgs).ToArray();
                    }
                }

                T instance;

                logger.LogVerbose("Invoking constructor: " + constructor);
                logger.Indent();

                try
                {
                    instance = (T)constructor.Invoke(args);
                }
                finally
                {
                    logger.Unindent();
                }

                logger.LogVerbose("Resolving properties:");
                logger.Indent();

                try
                {
                    ResolveDependencies(instance, dependencyChain);
                }
                finally
                {
                    logger.Unindent();
                }

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

            return new Factory(name, this, logger);
        }

        /// <summary>
        /// Register a typed dependency with the factory.
        /// </summary>
        public IFactory Dep<DepT>(DepT dependency)
        {
            Argument.NotNull(() => dependency);

            return Dep(typeof(DepT).Name, dependency);
        }

        /// <summary>
        /// Register a typed dependency with the factory.
        /// </summary>
        public IFactory Dep(Type dependencyType, object dependency)
        {
            Argument.NotNull(() => dependencyType);
            Argument.NotNull(() => dependency);

            return Dep(dependencyType.Name, dependency);
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

            return RemoveDep(dependencyType.Name);
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

                if (!IsDependencyRegistered(type.Name))
                {
                    if (!HasType(type.Name))
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
                        IsDependencyRegistered(parameter.ParameterType.Name) ||
                        HasType(parameter.ParameterType.Name)
                    )
                    .Select(parameter =>
                    {
                        if (IsDependencyRegistered(parameter.ParameterType.Name))
                        {
                            return parameter.Name + " (dependency: " + RetreiveDependency(parameter.ParameterType.Name).GetType().Name + ")";
                        }
                        else
                        {
                            return parameter.Name + " (factory creatable: " + FindType(parameter.ParameterType.Name).Name + ")";
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

                    logger.Indent();

                    logger.LogVerbose(property.Name + " => " + dependencyType.Name);

                    logger.Indent();

                    // Create and assign the dependency.
                    ResolvePropertyDep(instance, property, dependencyType, dependencyChain);

                    logger.Unindent();
                    logger.Unindent();
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

            var dependency = ResolveDep(dependencyType.Name, dependencyChain);
            property.SetValue(instance, dependency, null);
        }

        /// <summary>
        /// Resolve a dependency. Creates it or returns the cached version.
        /// </summary>
        public T ResolveDep<T>()
        {
            return (T)ResolveDep(typeof(T).Name);
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
            logger.Indent();

            try
            {
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
                    return InstantiateDependency(dependencyName, dependencyChain, dependencyType);
                }
                else if (fallbackFactory != null)
                {
                    dependencyType = fallbackFactory.FindType(dependencyName);
                    if (dependencyType != null)
                    {
                        return InstantiateDependency(dependencyName, dependencyChain, dependencyType);
                    }                    
                }

                throw new ApplicationException("Failed to find or create dependency " + dependencyName);
            }
            finally
            {
                logger.Unindent();
            }
        }

        /// <summary>
        /// Helper function to instantiate a dependency.
        /// </summary>
        private object InstantiateDependency(string dependencyName, Stack<Type> dependencyChain, Type dependencyType)
        {
            var singletonAttribute = ReflectionUtils.GetAttribute<LazySingletonAttribute>(dependencyType);
            if (singletonAttribute != null)
            {
                if (dependencyName == dependencyType.Name)
                {
                    throw new ApplicationException("Something unexpected happend, dependency name " + dependencyName + " and concrete dependency type " + dependencyType.Name + " are the same!");
                }

                // Dependency is a singleton.
                // Is the singleton already cached under the shared key?
                object cachedDependency;
                if (dependencies.TryGetValue(dependencyType.Name, out cachedDependency))
                {
                    // Yes, then cache it under the specified name.
                    Dep(dependencyName, cachedDependency);

                    return cachedDependency;
                }
            }

            var createdDependency = Instantiate<object>(dependencyType, emptyArray, dependencyChain);

            if (singletonAttribute != null)
            {
                if (dependencyName == dependencyType.Name)
                {
                    throw new ApplicationException("Something unexpected happend, dependency name " + dependencyName + " and concrete dependency type " + dependencyType.Name + " are the same!");
                }

                // Dependency is a singleton.
                // Cache the singleton that was just instantiated.
                Dep(dependencyName, createdDependency);         // Cache under interface name.                     
                Dep(dependencyType.Name, createdDependency);    // Cache under concrete type name as well.
            }

            return createdDependency;
        }

        /// <summary>
        /// Get the factory names for the specified type.
        /// </summary>
        public IEnumerable<string> GetFactoryNames<AttributeT>(Type type, Func<AttributeT, string> typeNameSelector)
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
        public IEnumerable<Type> OrderByDeps<AttributeT>(IEnumerable<Type> types, Func<AttributeT, string> typeNameSelector)
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
                    DetermineDeps(type)
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
        private IEnumerable<string> FindDeps(string typeName)
        {
            yield return typeName;

            var registeredType = FindType(typeName);
            if (registeredType != null)
            {
                // Merge in sub-dependencies.
                foreach (var subDep in DetermineDeps(registeredType))
                {
                    yield return subDep;
                }
            }
        }

        /// <summary>
        /// Determine the dependency tree for a particular type.
        /// </summary>
        private IEnumerable<string> DetermineDeps(Type type)
        {
            return type    // Start with types of properties.
                .GetProperties()
                .Where(p => ReflectionUtils.PropertyHasAttribute<DependencyAttribute>(p))
                .Select(p => p.PropertyType.Name)
                // Merge in constructor parameter types.
                .Concat(
                    type
                        .GetConstructors()
                        .SelectMany(c => c.GetParameters())
                        .Select(p => p.ParameterType.Name)
                )
                .Distinct()
                .SelectMany(n => FindDeps(n));
        }
    }
}
