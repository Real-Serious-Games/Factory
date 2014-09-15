using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utils;
using Utils.Dbg;

namespace Factory
{
    /// <summary>
    /// Helper class to scan dlls and find singleton types.
    /// </summary>
    public class SingletonScanner
    {
        /// <summary>
        /// Mockable access to C# reflection.
        /// </summary>
        private IReflection reflection;

        /// <summary>
        /// Logging facility.
        /// </summary>
        private ILogger logger;

        /// <summary>
        /// Singleton manager for registering singletons that are found.
        /// </summary>
        private ISingletonManager singletonManager;

        public SingletonScanner(IReflection reflection, ILogger logger, ISingletonManager singletonManager)
        {
            Argument.NotNull(() => reflection);
            Argument.NotNull(() => logger);
            Argument.NotNull(() => singletonManager);            

            this.reflection = reflection;
            this.logger = logger;
            this.singletonManager = singletonManager;
        }

        /// <summary>
        /// Scan for singleton types that are marked by either [Singleton] or [LazySingleton].
        /// </summary>
        public void ScanSingletonTypes()
        {
            var singletonTypes = reflection.FindTypesMarkedByAttributes(new Type[] { typeof(SingletonAttribute) });

            logger.LogInfo("Found singletons: " + singletonTypes.Select(t => t.Name).Join(", "));

            singletonTypes
                .Select(type =>
                    new SingletonDef()
                    {
                        singletonType = type,
                        dependencyNames = reflection
                            .GetAttributes<SingletonAttribute>(type)
                            .Where(attribute => attribute.DependencyName != null)
                            .Select(attribute => attribute.DependencyName)
                            .ToArray()
                    }
                )
                .Each(singletonDef => singletonManager.RegisterSingleton(singletonDef));            
        }
    }
}
