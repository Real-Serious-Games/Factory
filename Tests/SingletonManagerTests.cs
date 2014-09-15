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

        void Init()
        {
            mockFactory = new Mock<IFactory>();
            mockReflection = new Mock<IReflection>();
            mockLogger = new Mock<ILogger>();
            testObject = new SingletonManager(mockFactory.Object, mockReflection.Object, mockLogger.Object);
        }

        private void InitTestSingleton(Type singletonType, object singleton, params string[] dependencyNames)
        {
            testObject.RegisterSingleton(new SingletonDef()
            {
                singletonType = singletonType,
                dependencyNames = dependencyNames
            });

            mockFactory
                .Setup(m => m.Create(singletonType))
                .Returns(singleton);
        }

        [Fact]
        public void init_singletons_when_there_are_no_singletons()
        {
            Init();

            testObject.InstantiateSingletons();

            Assert.Empty(testObject.Singletons);
        }

        [Fact]
        public void can_find_instantiate_registered_singleton()
        {
            Init();

            var singletonType = typeof(object); // Anythin will do here.
            var singleton = new object();
            InitTestSingleton(singletonType, singleton);

            testObject.InstantiateSingletons();

            Assert.Equal(1, testObject.Singletons.Length);
            Assert.Equal(singleton, testObject.Singletons[0]);
        }

        [Fact]
        public void non_startable_singletons_are_ignored_on_start()
        {
            Init();

            var singletonType = typeof(object); // Anything will do here.
            var nonStartableSingleton = new object();
            InitTestSingleton(singletonType, nonStartableSingleton);

            testObject.InstantiateSingletons();

            testObject.Start();
        }

        [Fact]
        public void non_startable_singletons_are_ignored_on_shutdown()
        {
            Init();

            var singletonType = typeof(object); // Anything will do here.
            var nonStartableSingleton = new object();
            InitTestSingleton(singletonType, nonStartableSingleton);

            testObject.InstantiateSingletons();

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
            Init();

            var singletonType = typeof(object); // Anything will do here.
            var singleton = new object();
            var dependencyName = "dep1";
            InitTestSingleton(singletonType, singleton, dependencyName);

            testObject.InstantiateSingletons();

            Assert.Equal(singleton, testObject.ResolveDependency(dependencyName));
        }

        [Fact]
        public void can_start_singletons()
        {
            Init();

            var singletonType = typeof(object); // Anything will do here.
            var mockStartableSingleton = new Mock<IStartable>();
            InitTestSingleton(singletonType, mockStartableSingleton.Object);

            testObject.InstantiateSingletons();

            testObject.Start();

            mockStartableSingleton.Verify(m => m.Start(), Times.Once());
        }

        [Fact]
        public void can_shutdown_singletons()
        {
            Init();

            var singletonType = typeof(object); // Anything will do here.
            var mockStartableSingleton = new Mock<IStartable>();
            InitTestSingleton(singletonType, mockStartableSingleton.Object);

            testObject.InstantiateSingletons();

            testObject.Shutdown();

            mockStartableSingleton.Verify(m => m.Shutdown(), Times.Once());
        }

        [Fact]
        public void start_exceptions_are_swallowed_and_logged()
        {
            Init();

            var singletonType = typeof(object); // Anything will do here.
            var mockStartableSingleton = new Mock<IStartable>();
            InitTestSingleton(singletonType, mockStartableSingleton.Object);
            testObject.InstantiateSingletons();

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
            Init();

            var singletonType = typeof(object); // Anything will do here.
            var mockStartableSingleton = new Mock<IStartable>();
            InitTestSingleton(singletonType, mockStartableSingleton.Object);

            testObject.InstantiateSingletons();

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
