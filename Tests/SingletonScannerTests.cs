using Moq;
using RSG.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace RSG.Tests
{
    public class SingletonScannerTests
    {
        [Fact]
        public void can_handle_finding_no_singleton_types()
        {
            var mockReflection = new Mock<IReflection>();
            mockReflection
                .Setup(m => m.FindTypesMarkedByAttributes(new Type[] { typeof(SingletonAttribute) }))
                .Returns(new Type[0]);

            var mockLogger = new Mock<ILogger>();
            var mockSingletonManager = new Mock<ISingletonManager>();

            var testObject = new SingletonScanner(mockReflection.Object, mockLogger.Object, mockSingletonManager.Object);

            testObject.ScanSingletonTypes();

            mockSingletonManager.Verify(m => m.RegisterSingleton(It.IsAny<SingletonDef>()), Times.Never());
        }
        
        [Fact]
        public void can_find_and_register_singleton_type()
        {
            var singletonType = typeof(object); // Anything will do here.

            var mockReflection = new Mock<IReflection>();
            mockReflection
                .Setup(m => m.FindTypesMarkedByAttributes(new Type[] { typeof(SingletonAttribute) }))
                .Returns(new Type[] { singletonType });

            var mockLogger = new Mock<ILogger>();
            var mockSingletonManager = new Mock<ISingletonManager>();

            var testObject = new SingletonScanner(mockReflection.Object, mockLogger.Object, mockSingletonManager.Object);

            testObject.ScanSingletonTypes();

            mockSingletonManager.Verify(m => m.RegisterSingleton(It.Is<SingletonDef>(def =>
                def.singletonType == singletonType &&
                def.dependencyNames.Length == 0 &&
                !def.lazy
            )), Times.Once());
        }

        [Fact]
        public void can_find_and_register_lazy_singleton_type()
        {
            var singletonType = typeof(object); // Anything will do here.
            var dependencyName = "dep";

            var mockReflection = new Mock<IReflection>();
            mockReflection
                .Setup(m => m.FindTypesMarkedByAttributes(new Type[] { typeof(SingletonAttribute) }))
                .Returns(new Type[] { singletonType });
            mockReflection
                .Setup(m => m.GetAttributes<SingletonAttribute>(singletonType))
                .Returns(new SingletonAttribute[] { new LazySingletonAttribute(dependencyName) });

            var mockLogger = new Mock<ILogger>();
            var mockSingletonManager = new Mock<ISingletonManager>();

            var testObject = new SingletonScanner(mockReflection.Object, mockLogger.Object, mockSingletonManager.Object);

            testObject.ScanSingletonTypes();

            mockSingletonManager.Verify(m => m.RegisterSingleton(It.Is<SingletonDef>(def =>
                def.singletonType == singletonType &&
                def.lazy
            )), Times.Once());
        }

        [Fact]
        public void can_register_singleton_with_dependency_names()
        {
            var singletonType = typeof(object); // Anything will do here.
            var dependency1 = "dep1";
            var dependency2 = "dep2";

            var mockReflection = new Mock<IReflection>();
            mockReflection
                .Setup(m => m.FindTypesMarkedByAttributes(new Type[] { typeof(SingletonAttribute) }))
                .Returns(new Type[] { singletonType });
            mockReflection
                .Setup(m => m.GetAttributes<SingletonAttribute>(singletonType))
                .Returns(new SingletonAttribute[] { new SingletonAttribute(dependency1), new SingletonAttribute(dependency2) });

            var mockLogger = new Mock<ILogger>();
            var mockSingletonManager = new Mock<ISingletonManager>();

            var testObject = new SingletonScanner(mockReflection.Object, mockLogger.Object, mockSingletonManager.Object);

            testObject.ScanSingletonTypes();

            mockSingletonManager.Verify(m => m.RegisterSingleton(It.Is<SingletonDef>(def =>
                def.singletonType == singletonType &&
                def.dependencyNames.Length == 2 &&
                def.dependencyNames.Contains(dependency1) &&
                def.dependencyNames.Contains(dependency2)
            )), Times.Once());
        }

        [Fact]
        public void disabled_singletons_are_ignored()
        {
            var singletonType = typeof(object); // Anything will do here.

            var mockReflection = new Mock<IReflection>();
            mockReflection
                .Setup(m => m.FindTypesMarkedByAttributes(new Type[] { typeof(SingletonAttribute) }))
                .Returns(new Type[] { singletonType });

            var testSingletonAttribute = new SingletonAttribute(() => false);
            
            mockReflection
                .Setup(m => m.GetAttributes<SingletonAttribute>(singletonType))
                .Returns(new SingletonAttribute[] { testSingletonAttribute }); ;

            var mockLogger = new Mock<ILogger>();
            var mockSingletonManager = new Mock<ISingletonManager>();

            var testObject = new SingletonScanner(mockReflection.Object, mockLogger.Object, mockSingletonManager.Object);

            testObject.ScanSingletonTypes();

            mockSingletonManager.Verify(m => m.RegisterSingleton(It.IsAny<SingletonDef>()), Times.Never());
        }
    }

}
