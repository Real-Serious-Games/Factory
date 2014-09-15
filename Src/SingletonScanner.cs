using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utils;
using Utils.Dbg;

namespace Utils
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

            var singletonDefs = singletonTypes
                .Select(type =>
                {
                    var attrs = reflection.GetAttributes<SingletonAttribute>(type).ToArray();

                    return new SingletonDef()
                    {
                        singletonType = type,
                        dependencyNames = attrs
                            .Where(attribute => attribute.DependencyName != null)
                            .Select(attribute => attribute.DependencyName)
                            .ToArray(),
                        lazy = attrs.Any(atribute => atribute.Lazy)
                    };
                })
                .ToArray();

            logger.LogInfo("Found singletons: \n" + singletonDefs.Select(def => def.ToString()).Join("\n"));

            singletonDefs
                .Each(singletonDef => singletonManager.RegisterSingleton(singletonDef));            
        }
    }
}
