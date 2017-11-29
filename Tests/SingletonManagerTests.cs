using Moq;
using RSG.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace RSG.Tests
{
    public class SingletonManagerTests
    {
        static Mock<ILogger> mockLogger;
        static Mock<IFactory> mockFactory;
        static SingletonManager testObject;

        static void Init()
        {
            mockFactory = new Mock<IFactory>();
            mockLogger = new Mock<ILogger>();
            testObject = new SingletonManager(mockLogger.Object, mockFactory.Object);
        }

        static void InitTestSingleton(Type singletonType, object singleton, params string[] dependencyNames)
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

            testObject.InstantiateSingletons(mockFactory.Object);

            Assert.Empty(testObject.Singletons);
        }

        [Fact]
        public void can_instantiate_registered_singleton()
        {
            Init();

            var singletonType = typeof(object); // Anything will do here.
            var singleton = new object();
            InitTestSingleton(singletonType, singleton);

            testObject.InstantiateSingletons(mockFactory.Object);

            Assert.Equal(1, testObject.Singletons.Length);
            Assert.Equal(singleton, testObject.Singletons[0]);
        }

        [Fact]
        public void can_instantiate_singleton_with_custom_method()
        {
            Init();

            var singleton = new object();
            testObject.RegisterSingleton(new SingletonDef()
            {
                singletonType = typeof(object),
                dependencyNames = new string[0],
                Instantiate = (f, t) => singleton
            });

            testObject.InstantiateSingletons(mockFactory.Object);

            Assert.Equal(1, testObject.Singletons.Length);
            Assert.Equal(singleton, testObject.Singletons[0]);
        }

        [Fact]
        public void exception_thrown_by_custom_instantiator_is_handled()
        {
            Init();

            var ex = new ApplicationException("!!!");

            var singleton = new object();
            testObject.RegisterSingleton(new SingletonDef()
            {
                singletonType = typeof(object),
                dependencyNames = new string[0],
                Instantiate = (f, t) => 
                {
                    throw ex;
                }
            });

            testObject.InstantiateSingletons(mockFactory.Object);

            mockLogger.Verify(m => m.LogError(ex, It.IsAny<string>(), It.IsAny<object[]>()), Times.Once());

            Assert.Equal(0, testObject.Singletons.Length);
        }

        public class can_instantiate_dependent_singletons
        {
            public interface ITest1
            {

            }

            public class Test1 : ITest1
            {

            }

            public class Test2
            {
                [Dependency]
                public ITest1 Test1 { get; set; }
            }


            [Fact]
            public void when_in_correct_order()
            {
                Init();

                var singletonType1 = typeof(Test1);
                var singleton1 = new Test1();
                InitTestSingleton(singletonType1, singleton1, typeof(ITest1).Name);

                var singletonType2 = typeof(Test2);
                var singleton2 = new Test2();
                InitTestSingleton(singletonType2, singleton2);

                testObject.InstantiateSingletons(mockFactory.Object);

                Assert.Equal(2, testObject.Singletons.Length);
                Assert.Equal(singleton1, testObject.Singletons[0]);
                Assert.Equal(singleton2, testObject.Singletons[1]);
            }

            [Fact]
            public void when_out_of_order()
            {
                Init();

                var singletonType2 = typeof(Test2);
                var singleton2 = new Test2();
                InitTestSingleton(singletonType2, singleton2);

                var singletonType1 = typeof(Test1);
                var singleton1 = new Test1();
                InitTestSingleton(singletonType1, singleton1, typeof(ITest1).Name);

                testObject.InstantiateSingletons(mockFactory.Object);

                Assert.Equal(2, testObject.Singletons.Length);
                Assert.Equal(singleton1, testObject.Singletons[0]);
                Assert.Equal(singleton2, testObject.Singletons[1]);
            }
        }

        public class shutdown_is_performed_in_reverse_order_to_startup
        {
            public interface ITest1
            {
                bool ShutdownPerformed { get; }
            }

            public class Test1 : ITest1, IStartable
            {
                public void Shutdown()
                {
                    ShutdownPerformed = true;
                }

                public void Startup()
                {
                    ShutdownPerformed = false;
                }

                public bool ShutdownPerformed { get; private set; }
            }

            public class Test2 : IStartable
            {
                [Dependency]
                public ITest1 Test1 { get; set; }

                public bool ShutdownPerformedCorrectly { get; private set; }

                public void Shutdown()
                {
                    Assert.False(Test1.ShutdownPerformed);
                    ShutdownPerformedCorrectly = true;
                }

                public void Startup()
                {
                    ShutdownPerformedCorrectly = false;
                }
            }

            [Fact]
            public void when_in_correct_order()
            {
                Init();

                var singletonType1 = typeof(Test1);
                var singleton1 = new Test1();
                InitTestSingleton(singletonType1, singleton1, typeof(ITest1).Name);

                var singletonType2 = typeof(Test2);
                var singleton2 = new Test2();
                InitTestSingleton(singletonType2, singleton2);

                testObject.InstantiateSingletons(mockFactory.Object);

                singleton2.Test1 = singleton1;

                testObject.Shutdown();

                Assert.True(singleton2.ShutdownPerformedCorrectly);
            }

            [Fact]
            public void when_out_of_order()
            {
                Init();

                var singletonType2 = typeof(Test2);
                var singleton2 = new Test2();
                InitTestSingleton(singletonType2, singleton2);

                var singletonType1 = typeof(Test1);
                var singleton1 = new Test1();
                InitTestSingleton(singletonType1, singleton1, typeof(ITest1).Name);

                testObject.InstantiateSingletons(mockFactory.Object);

                singleton2.Test1 = singleton1;

                testObject.Shutdown();

                Assert.True(singleton2.ShutdownPerformedCorrectly);
            }
        }
        
        [Fact]
        public void lazy_singleton_is_instantiated_on_resolve()
        {
            Init();

            var singletonType = typeof(object); // Anything will do here.
            var dependencyName = "dep";

            testObject.RegisterSingleton(new SingletonDef()
            {
                singletonType = singletonType,
                dependencyNames = new string[] { dependencyName },
                lazy = true
            });

            testObject.InstantiateSingletons(mockFactory.Object);

            var singleton = new object();
            mockFactory
                .Setup(m => m.Create(singletonType))
                .Returns(singleton);

            Assert.Equal(singleton, testObject.ResolveDependency(dependencyName));
        }

        [Fact]
        public void subsequent_resolve_of_lazy_singleton_gets_cached_object()
        {
            Init();

            var singletonType = typeof(object); // Anything will do here.
            var dependencyName = "dep";

            testObject.RegisterSingleton(new SingletonDef()
            {
                singletonType = singletonType,
                dependencyNames = new string[] { dependencyName },
                lazy = true
            });

            testObject.InstantiateSingletons(mockFactory.Object);

            var singleton = new object();
            mockFactory
                .Setup(m => m.Create(singletonType))
                .Returns(singleton);

            Assert.Equal(singleton, testObject.ResolveDependency(dependencyName));
            Assert.Equal(singleton, testObject.ResolveDependency(dependencyName));

            mockFactory.Verify(m => m.Create(singletonType), Times.Once());
        }

        [Fact]
        public void can_get_type_of_singleton()
        {
            Init();

            var singletonType = typeof(object); // Anything will do here.
            var dependencyName = "dep";

            testObject.RegisterSingleton(new SingletonDef()
            {
                singletonType = singletonType,
                dependencyNames = new string[] { dependencyName },
                lazy = false
            });

            testObject.InstantiateSingletons(mockFactory.Object);

            Assert.Equal(singletonType, testObject.FindDependencyType(dependencyName));
        }

        [Fact]
        public void can_get_type_of_lazy_singleton()
        {
            Init();

            var singletonType = typeof(object); // Anything will do here.
            var dependencyName = "dep";

            testObject.RegisterSingleton(new SingletonDef()
            {
                singletonType = singletonType,
                dependencyNames = new string[] { dependencyName },
                lazy = true
            });

            testObject.InstantiateSingletons(mockFactory.Object);

            Assert.Equal(singletonType, testObject.FindDependencyType(dependencyName));
        }


        [Fact]
        public void non_startable_singletons_are_ignored_on_start()
        {
            Init();

            var singletonType = typeof(object); // Anything will do here.
            var nonStartableSingleton = new object();
            InitTestSingleton(singletonType, nonStartableSingleton);

            testObject.InstantiateSingletons(mockFactory.Object);

            testObject.Startup();
        }

        [Fact]
        public void non_startable_singletons_are_ignored_on_shutdown()
        {
            Init();

            var singletonType = typeof(object); // Anything will do here.
            var nonStartableSingleton = new object();
            InitTestSingleton(singletonType, nonStartableSingleton);

            testObject.InstantiateSingletons(mockFactory.Object);

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

            testObject.InstantiateSingletons(mockFactory.Object);

            Assert.Equal(singleton, testObject.ResolveDependency(dependencyName));
        }

        [Fact]
        public void can_start_singletons()
        {
            Init();

            var singletonType = typeof(object); // Anything will do here.
            var mockStartableSingleton = new Mock<IStartable>();
            InitTestSingleton(singletonType, mockStartableSingleton.Object);

            testObject.InstantiateSingletons(mockFactory.Object);

            testObject.Startup();

            mockStartableSingleton.Verify(m => m.Startup(), Times.Once());
        }

        [Fact]
        public void can_shutdown_singletons()
        {
            Init();

            var singletonType = typeof(object); // Anything will do here.
            var mockStartableSingleton = new Mock<IStartable>();
            InitTestSingleton(singletonType, mockStartableSingleton.Object);

            testObject.InstantiateSingletons(mockFactory.Object);

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
            testObject.InstantiateSingletons(mockFactory.Object);

            mockStartableSingleton
                .Setup(m => m.Startup())
                .Throws<ApplicationException>();

            Assert.DoesNotThrow(() => 
                testObject.Startup()
            );

            mockLogger.Verify(m => m.LogError(It.IsAny<ApplicationException>(), It.IsAny<string>()), Times.Once());
        }

        [Fact]
        public void shutdown_exceptions_are_swallowed_and_logged()
        {
            Init();

            var singletonType = typeof(object); // Anything will do here.
            var mockStartableSingleton = new Mock<IStartable>();
            InitTestSingleton(singletonType, mockStartableSingleton.Object);

            testObject.InstantiateSingletons(mockFactory.Object);

            mockStartableSingleton
                .Setup(m => m.Shutdown())
                .Throws<ApplicationException>();

            Assert.DoesNotThrow(() =>
                testObject.Shutdown()
            );

            mockLogger.Verify(m => m.LogError(It.IsAny<ApplicationException>(), It.IsAny<string>()), Times.Once());
        }

        private static SingletonDef InitDef(Type singletonType, params Type[] interfaceTypes)
        {
            return new SingletonDef()
            {
                singletonType = singletonType,
                dependencyNames = interfaceTypes.Select(type => type.Name).ToArray()
            };
        }

        public class can_order_types_based_on_dependencies_when_types_are_already_in_correct_order
        {
            public interface ITest1
            {

            }

            public class Test1 : ITest1
            {

            }

            public class Test2
            {
                [Dependency]
                public ITest1 Test1 { get; set; }
            }

            [Fact(Timeout = 1000)]
            public void test()
            {
                var mockFactory = new Mock<IFactory>();

                var def1 = InitDef(typeof(Test1), typeof(ITest1));
                var def2 = InitDef(typeof(Test2));
                var singletonDefs = new SingletonDef[] { def1, def2 };

                var sorted = SingletonManager.OrderByDeps(singletonDefs, mockFactory.Object, new Mock<ILogger>().Object).ToArray();

                Assert.Equal(2, sorted.Length);
                Assert.Equal(def1, sorted[0]);
                Assert.Equal(def2, sorted[1]);
            }
        }

        public class can_order_types_based_on_dependencies_with_two_types_out_of_order
        {
            public interface ITest1
            {

            }

            public class Test1 : ITest1
            {

            }

            public class Test2
            {
                [Dependency]
                public ITest1 Test1 { get; set; }
            }

            [Fact(Timeout = 1000)]
            public void test()
            {
                var mockFactory = new Mock<IFactory>();

                var def1 = InitDef(typeof(Test2));
                var def2 = InitDef(typeof(Test1), typeof(ITest1));
                var singletonDefs = new SingletonDef[] { def1, def2 };

                var sorted = SingletonManager.OrderByDeps(singletonDefs, mockFactory.Object, new Mock<ILogger>().Object).ToArray();

                Assert.Equal(2, sorted.Length);
                Assert.Equal(def2, sorted[0]);
                Assert.Equal(def1, sorted[1]);
            }
        }

        public class can_order_types_based_on_dependencies_with_multiple_dependent_types
        {
            public interface ITest1
            {

            }

            public class Test1 : ITest1
            {

            }

            public interface ITest2
            {

            }

            public class Test2 : ITest2
            {

            }

            public class Test3
            {
                [Dependency]
                public ITest1 Test1 { get; set; }

                [Dependency]
                public ITest2 Test2 { get; set; }
            }

            [Fact(Timeout = 1000)]
            public void test()
            {
                var mockFactory = new Mock<IFactory>();

                var def1 = InitDef(typeof(Test1), typeof(ITest1));
                var def2 = InitDef(typeof(Test3));
                var def3 = InitDef(typeof(Test2), typeof(ITest2));
                var singletonDefs = new SingletonDef[] { def1, def2, def3 };

                var sorted = SingletonManager.OrderByDeps(singletonDefs, mockFactory.Object, new Mock<ILogger>().Object).ToArray();

                Assert.Equal(3, sorted.Length);
                Assert.Equal(def1, sorted[0]);
                Assert.Equal(def3, sorted[1]);
                Assert.Equal(def2, sorted[2]);
            }
        }

        public class can_order_types_based_on_property_dependencies_with_intermediate_type
        {
            public interface ITest1
            {

            }

            public class Test1 : ITest1
            {

            }

            public interface ITest2
            {

            }

            public class Test2 : ITest2
            {
                [Dependency]
                public ITest1 Test1 { get; set; }
            }

            public class Test3
            {
                [Dependency]
                public ITest2 Test2 { get; set; }
            }

            [Fact(Timeout = 1000)]
            public void test()
            {
                var mockFactory = new Mock<IFactory>();

                mockFactory
                    .Setup(m => m.FindType(typeof(ITest2).Name))
                    .Returns(typeof(Test2));

                var def1 = InitDef(typeof(Test3));
                var def2 = InitDef(typeof(Test1), typeof(ITest1));
                var singletonDefs = new SingletonDef[] { def1, def2 };

                var sorted = SingletonManager.OrderByDeps(singletonDefs, mockFactory.Object, new Mock<ILogger>().Object).ToArray();

                Assert.Equal(2, sorted.Length);
                Assert.Equal(def2, sorted[0]);
                Assert.Equal(def1, sorted[1]);
            }
        }
        public class can_order_types_with_unspecified_dependencies
        {
            public interface ITest1
            {

            }

            public class Test2
            {
            }

            public class Test3
            {
                [Dependency]
                public ITest1 Test1 { get; set; }
            }

            [Fact(Timeout = 1000)]
            public void test()
            {
                var mockFactory = new Mock<IFactory>();

                var def1 = InitDef(typeof(Test2));
                var def2 = InitDef(typeof(Test3));
                var singletonDefs = new SingletonDef[] { def1, def2 };

                var sorted = SingletonManager.OrderByDeps(singletonDefs, mockFactory.Object, new Mock<ILogger>().Object).ToArray();

                Assert.Equal(2, sorted.Length);
                Assert.Equal(def1, sorted[0]);
                Assert.Equal(def2, sorted[1]);
            }

            [Fact(Timeout = 1000)]
            public void reverse()
            {
                var mockFactory = new Mock<IFactory>();

                var def1 = InitDef(typeof(Test3));
                var def2 = InitDef(typeof(Test2));
                var singletonDefs = new SingletonDef[] { def1, def2 };

                var sorted = SingletonManager.OrderByDeps(singletonDefs, mockFactory.Object, new Mock<ILogger>().Object).ToArray();

                Assert.Equal(2, sorted.Length);
                Assert.Equal(def1, sorted[0]);
                Assert.Equal(def2, sorted[1]);
            }
        }

        public class can_order_types_based_on_constructor_dependencies_with_multiple_dependent_types
        {
            public interface ITest1
            {

            }

            public class Test1 : ITest1
            {

            }

            public interface ITest2
            {

            }

            public class Test2 : ITest2
            {

            }

            public class Test3
            {
                public Test3(ITest1 test1, ITest2 test2)
                {
                    this.Test1 = test1;
                    this.Test2 = Test2;
                }

                public ITest1 Test1 { get; set; }
                public ITest2 Test2 { get; set; }
            }

            [Fact(Timeout = 1000)]
            public void test()
            {
                var types = new Type[] { typeof(Test1), typeof(Test3), typeof(Test2) };

                var mockLogger = new Mock<ILogger>();
                var mockFactory = new Mock<IFactory>();

                var def1 = InitDef(typeof(Test1), typeof(ITest1));
                var def2 = InitDef(typeof(Test3));
                var def3 = InitDef(typeof(Test2), typeof(ITest2));
                var singletonDefs = new SingletonDef[] { def1, def2, def3 };
                                
                var sorted = SingletonManager.OrderByDeps(singletonDefs, mockFactory.Object, new Mock<ILogger>().Object).ToArray();

                Assert.Equal(3, sorted.Length);
                Assert.Equal(def1, sorted[0]);
                Assert.Equal(def3, sorted[1]);
                Assert.Equal(def2, sorted[2]);
            }
        }

        public class throws_when_attempting_to_order_types_that_have_circular_dependency
        {
            public interface ITest1
            {

            }

            public class Test1 : ITest1
            {
                [Dependency]
                public ITest2 Test2 { get; set; }
            }

            public interface ITest2
            {

            }

            public class Test2 : ITest2
            {
                [Dependency]
                public ITest1 Test1 { get; set; }
            }

            [Fact(Timeout = 1000)]
            public void test()
            {
                var mockFactory = new Mock<IFactory>();

                var def1 = InitDef(typeof(Test1), typeof(ITest1));
                var def2 = InitDef(typeof(Test2), typeof(ITest2));
                var singletonDefs = new SingletonDef[] { def1, def2 };

                Assert.Throws<ApplicationException>(() =>
                    SingletonManager.OrderByDeps(singletonDefs, mockFactory.Object, new Mock<ILogger>().Object)
                );
            }
        }
    }
}
