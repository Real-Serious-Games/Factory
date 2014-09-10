using Core;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utils;
using Xunit;

namespace Utils.Tests
{
    public class SingletonManagerTests
    {
        Mock<ILogger> mockLogger;
        Mock<IFactory> mockFactory;
        Mock<IReflection> mockReflection;
        SingletonManager testObject;

        void Init(params object[] singletons)
        {
            mockFactory = new Mock<IFactory>();
            mockReflection = new Mock<IReflection>();
            mockLogger = new Mock<ILogger>();
            testObject = new SingletonManager(mockFactory.Object, mockReflection.Object, mockLogger.Object);


            var singletonTypes = singletons.Select(s => s.GetType()).ToArray();
            mockReflection
                .Setup(m => m.FindTypesMarkedByAttributes(new Type[] { typeof(SingletonAttribute) }))
                .Returns(singletonTypes);
            mockFactory
                .Setup(m => m.OrderByDeps<SingletonAttribute>(singletonTypes, It.IsAny<Func<SingletonAttribute, string>>()))
                .Returns(singletonTypes);

            singletons.Each(singleton =>
            {
                var singletonType = singleton.GetType();
                mockReflection
                    .Setup(m => m.GetAttributes<SingletonAttribute>(singletonType))
                    .Returns(new SingletonAttribute[] { new SingletonAttribute(singleton.GetType().Name) });
                mockFactory
                    .Setup(m => m.Create(singletonType))
                    .Returns(singleton);
            });

            testObject.InitSingletons();
        }

        [Fact]
        public void init_singletons_when_there_are_no_singletons()
        {
            Init();

            testObject.InitSingletons();

            Assert.Empty(testObject.Singletons);
        }

        [Fact]
        public void can_find_singleton()
        {
            var singleton = new object();

            Init(singleton);

            Assert.Equal(1, testObject.Singletons.Length);
            Assert.Equal(singleton, testObject.Singletons[0]);
        }

        [Fact]
        public void non_startable_singletons_are_ignored_on_start()
        {
            var nonStartableSingleton = new object();

            Init(nonStartableSingleton);

            testObject.Start();
        }

        [Fact]
        public void non_startable_singletons_are_ignored_on_shutdown()
        {
            var nonStartableSingleton = new object();

            Init(nonStartableSingleton);

            testObject.Shutdown();
        }

        [Fact]
        public void resolving_non_existant_singleton_returns_null()
        {
            Init();

            Assert.Null(testObject.ResolveDependency("some singleton that doesnt exist"));
        }

        [Fact]
        public void can_resolve_singleton_as_dependency()
        {
            var singleton = new object();

            Init(singleton);

            Assert.Equal(singleton, testObject.ResolveDependency(singleton.GetType().Name));
        }

        [Fact]
        public void can_start_singletons()
        {
            var mockStartableSingleton = new Mock<IStartable>();

            Init(mockStartableSingleton.Object);

            testObject.Start();

            mockStartableSingleton.Verify(m => m.Start(), Times.Once());
        }

        [Fact]
        public void can_shutdown_singletons()
        {
            var mockStartableSingleton = new Mock<IStartable>();

            Init(mockStartableSingleton.Object);

            testObject.Shutdown();

            mockStartableSingleton.Verify(m => m.Shutdown(), Times.Once());
        }

        [Fact]
        public void start_exceptions_are_swallowed_and_logged()
        {
            var mockStartableSingleton = new Mock<IStartable>();

            Init(mockStartableSingleton.Object);

            mockStartableSingleton
                .Setup(m => m.Start())
                .Throws<ApplicationException>();

            Assert.DoesNotThrow(() => 
                testObject.Start()
            );

            mockLogger.Verify(m => m.LogError(It.IsAny<string>(), It.IsAny<ApplicationException>()), Times.Once());            
        }

        [Fact]
        public void shutdown_exceptions_are_swallowed_and_logged()
        {
            var mockStartableSingleton = new Mock<IStartable>();

            Init(mockStartableSingleton.Object);

            mockStartableSingleton
                .Setup(m => m.Shutdown())
                .Throws<ApplicationException>();

            Assert.DoesNotThrow(() =>
                testObject.Shutdown()
            );

            mockLogger.Verify(m => m.LogError(It.IsAny<string>(), It.IsAny<ApplicationException>()), Times.Once());
        }
    }
}
