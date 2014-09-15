using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utils;
using Xunit;

namespace Factory.Tests
{
    public class SingletonScannerTests
    {
        //todo: test with no singleton types
                //todo: test with dependency names

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
        public void can_find_singleton_type_marked_by_attribute()
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
                def.dependencyNames.Length == 0
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
    }

}
