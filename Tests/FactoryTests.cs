using Moq;
using RSG.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace RSG.Factory.Tests
{
    public class FactoryTests
    {
        [Fact]
        public void throws_exception_on_request_for_unknown_type()
        {
            var mockLogger = new Mock<ILogger>();
            var testObject = new Factory("test", mockLogger.Object);

            Assert.Throws<ApplicationException>(() =>
            {
                testObject.Create<object>("bad_type");
            });
        }

        [Fact]
        public void factory_registers_itself_for_injection()
        {
            var mockLogger = new Mock<ILogger>();
            var testObject = new Factory("test", mockLogger.Object);

            Assert.Equal(testObject, testObject.RetreiveDependency<IFactory>());
        }

        [Fact]
        public void factory_with_fallback_registers_itself_for_injection()
        {
            var mockLogger = new Mock<ILogger>();
            var mockFallback = new Mock<IFactory>();

            var testObject = new Factory("test", mockFallback.Object, mockLogger.Object);

            Assert.Equal(testObject, testObject.RetreiveDependency<IFactory>());
        }

        public class query_for_non_existant_type
        {
            public interface ITest
            {
            }

            [Fact]
            public void test()
            {
                var mockLogger = new Mock<ILogger>();

                var testObject = new Factory("test", mockLogger.Object);

                Assert.Null(testObject.FindType("foo"));
                Assert.Null(testObject.FindType<ITest>());
            }
        }

        public class can_add_type_by_name
        {
            public interface ITest
            {

            }

            public class Test : ITest
            {
            }

            [Fact]
            public void test()
            {
                var mockLogger = new Mock<ILogger>();

                var testObject = new Factory("test", mockLogger.Object);

                var typeName = "foo";
                var type = typeof(Test);
                testObject.Type(typeName, type);

                Assert.Equal(type, testObject.FindType(typeName));
            }
        }

        public class can_add_type_by_interface
        {
            public interface ITest
            {

            }

            public class Test : ITest
            {
            }

            [Fact]
            public void test()
            {
                var mockLogger = new Mock<ILogger>();

                var testObject = new Factory("test", mockLogger.Object);

                var type = typeof(Test);
                testObject.Type<ITest>(type);

                Assert.Equal(type, testObject.FindType<ITest>());
            }
        }

        [Fact]
        public void cant_find_non_existant_type()
        {
            var mockLogger = new Mock<ILogger>();
            var testObject = new Factory("test", mockLogger.Object);

            Assert.Null(testObject.FindType("TestClass"));
        }

        public class can_find_type_from_fallback
        {
            public interface ITest
            {

            }

            public class Test : ITest
            {
            }

            [Fact]
            public void test()
            {
                var mockFallback = new Mock<IFactory>();
                var mockLogger = new Mock<ILogger>();
                var testObject = new Factory("test", mockFallback.Object, mockLogger.Object);

                var typeName = "foo";
                var type = typeof(Test);
                mockFallback
                    .Setup(m => m.FindType(typeName))
                    .Returns(type);

                Assert.Equal(type, testObject.FindType(typeName));
            }
        }

        public class can_find_type_from_dependency_plugin
        {
            public interface ITest
            {

            }

            public class Test : ITest
            {
            }

            [Fact]
            public void test()
            {
                var mockProvider = new Mock<IDependencyProvider>();
                var mockLogger = new Mock<ILogger>();
                
                var testObject = new Factory("test", mockLogger.Object);
                testObject.AddDependencyProvider(mockProvider.Object);

                var typeName = "foo";
                var type = typeof(Test);
                mockProvider
                    .Setup(m => m.FindDependencyType(typeName))
                    .Returns(type);

                Assert.Equal(type, testObject.FindType(typeName));
            }
        }

        public class can_create_named_object
        {
            public class Test
            {

            }

            [Fact]
            public void test()
            {
                var mockLogger = new Mock<ILogger>();

                var testObject = new Factory("test", mockLogger.Object);

                var typeName = "SomeType";
                var type = typeof(Test);

                testObject.Type(typeName, type);

                var obj = testObject.Create<Test>(typeName);

                Assert.NotNull(obj);
                Assert.IsType(type, obj);
            }
        }

        public class can_create_named_object_from_fallback
        {
            public class Test
            {

            }

            [Fact]
            public void test()
            {
                var mockFallback = new Mock<IFactory>();
                var mockLogger = new Mock<ILogger>();

                var testObject = new Factory("test", mockFallback.Object, mockLogger.Object);

                var typeName = "SomeType";
                var type = typeof(Test);

                mockFallback
                    .Setup(m => m.FindType(typeName))
                    .Returns(type);

                var obj = testObject.Create<Test>(typeName);

                Assert.NotNull(obj);
                Assert.IsType(type, obj);
            }
        }

        public class can_create_named_object_and_cast_to_interface
        {
            public interface ITest
            {

            }

            public class Test : ITest
            {

            }

            [Fact]
            public void test()
            {
                var mockLogger = new Mock<ILogger>();

                var testObject = new Factory("test", mockLogger.Object);

                var typeName = "SomeType";
                var type = typeof(Test);

                testObject.Type(typeName, type);

                var obj = testObject.Create<ITest>(typeName);

                Assert.NotNull(obj);
                Assert.IsType(type, obj);
            }
        }

        public class throws_exception_when_creating_object_that_doesnt_implement_specified_interface
        {
            public interface ITest
            {

            }

            public class Test
            {

            }

            [Fact]
            public void test()
            {
                var mockLogger = new Mock<ILogger>();

                var testObject = new Factory("test", mockLogger.Object);

                var typeName = "SomeType";
                var type = typeof(Test);

                testObject.Type(typeName, type);

                Assert.Throws<InvalidCastException>(() =>
                    testObject.Create<ITest>(typeName)
                );
            }
        }

        public class can_create_object_specified_by_interface
        {
            public interface ITest
            {

            }

            public class Test : ITest
            {

            }

            [Fact]
            public void test()
            {
                var mockLogger = new Mock<ILogger>();

                var testObject = new Factory("test", mockLogger.Object);

                var type = typeof(Test);

                var factoryCreatableAttribute = new FactoryCreatableAttribute(typeof(ITest));

                testObject.Type(factoryCreatableAttribute.TypeName, type);

                var obj = testObject.CreateInterface<ITest>();

                Assert.NotNull(obj);
                Assert.IsType(type, obj);
            }
        }

        public class can_create_generic_type_specified_by_interface
        {
            public interface ITest<T1, T2>
            {

            }

            public class Test<T1, T2> : ITest<T1, T2>
            {

            }

            [Fact]
            public void test()
            {
                var mockLogger = new Mock<ILogger>();

                var testObject = new Factory("test", mockLogger.Object);

                var type = typeof(Test<int, string>);

                var factoryCreatableAttribute = new FactoryCreatableAttribute(typeof(ITest<int, string>));

                testObject.Type(factoryCreatableAttribute.TypeName, type);

                var obj = testObject.CreateInterface<ITest<int, string>>();

                Assert.NotNull(obj);
                Assert.IsType(type, obj);
            }
        }

        public class factory_type_with_different_generic_parameters_doesnt_conflict
        {
            public interface ITest<T>
            {

            }

            public class Test<T> : ITest<T>
            {

            }

            [Fact]
            public void test()
            {
                var mockLogger = new Mock<ILogger>();

                var testObject = new Factory("test", mockLogger.Object);

                var type1 = typeof(Test<int>);
                var type2 = typeof(Test<string>);

                var factoryCreatableAttribute1 = new FactoryCreatableAttribute(typeof(ITest<int>));
                var factoryCreatableAttribute2 = new FactoryCreatableAttribute(typeof(ITest<string>));

                testObject.Type(factoryCreatableAttribute1.TypeName, type1);
                testObject.Type(factoryCreatableAttribute2.TypeName, type2);

                Assert.IsType(type1, testObject.CreateInterface<ITest<int>>());
                Assert.IsType(type2, testObject.CreateInterface<ITest<string>>());
            }
        }


        public class throws_exception_when_creating_object_by_interface_that_doesnt_implement_the_specified_interface
        {
            public interface ITest
            {

            }

            public class Test
            {

            }

            [Fact]
            public void test()
            {
                var mockLogger = new Mock<ILogger>();

                var testObject = new Factory("test", mockLogger.Object);

                var type = typeof(Test);

                var factoryCreatableAttribute = new FactoryCreatableAttribute(typeof(ITest));

                testObject.Type(factoryCreatableAttribute.TypeName, type);

                Assert.Throws<InvalidCastException>(() =>
                    testObject.CreateInterface<ITest>()
                );
            }
        }

        public class can_create_object_specified_by_type
        {
            public class Test
            {

            }

            [Fact]
            public void test()
            {
                var mockLogger = new Mock<ILogger>();

                var testObject = new Factory("test", mockLogger.Object);

                var type = typeof(Test);
                var obj = testObject.Create(type);

                Assert.NotNull(obj);
                Assert.IsType(type, obj);
            }
        }

        public class can_create_view_from_data
        {
            public class TestData
            {

            }

            public interface ITestView
            {

            }

            public class TestView : ITestView
            {

            }

            [Fact]
            public void test()
            {
                var mockLogger = new Mock<ILogger>();

                var testObject = new Factory("test", mockLogger.Object);

                var viewType = typeof(TestView);
                var viewInterfaceType = typeof(ITestView);
                var dataType = typeof(TestData);
                var factoryCreatableAttribute = new FactoryCreatableAttribute(viewInterfaceType, dataType);

                testObject.Type(factoryCreatableAttribute.TypeName, viewType);

                var view = testObject.CreateView<ITestView>(dataType);

                Assert.NotNull(view);
                Assert.IsType(viewType, view);
            }
        }

        public class can_create_view_from_data_with_interface
        {
            public interface ITestData
            {
            }

            public class TestData : ITestData
            {

            }

            public interface ITestView
            {

            }

            public class TestView : ITestView
            {

            }

            [Fact]
            public void test()
            {
                var mockLogger = new Mock<ILogger>();

                var testObject = new Factory("test", mockLogger.Object);

                var viewType = typeof(TestView);
                var viewInterfaceType = typeof(ITestView);
                var dataType = typeof(ITestData);
                var factoryCreatableAttribute = new FactoryCreatableAttribute(viewInterfaceType, dataType);

                testObject.Type(factoryCreatableAttribute.TypeName, viewType);

                var view = testObject.CreateView<ITestView>(dataType);

                Assert.NotNull(view);
                Assert.IsType(viewType, view);
            }
        }

        public class can_create_view_from_data_with_multiple_interface
        {
            public interface ITestData1
            {
            }

            public interface ITestData2
            {
            }

            public class TestData : ITestData1, ITestData2
            {

            }

            public interface ITestView
            {

            }

            public class TestView : ITestView
            {

            }

            [Fact]
            public void test()
            {
                var mockLogger = new Mock<ILogger>();

                var testObject = new Factory("test", mockLogger.Object);

                var viewType = typeof(TestView);
                var viewInterfaceType = typeof(ITestView);
                var dataType = typeof(ITestData2);
                var factoryCreatableAttribute = new FactoryCreatableAttribute(viewInterfaceType, dataType);

                testObject.Type(factoryCreatableAttribute.TypeName, viewType);

                var view = testObject.CreateView<ITestView>(dataType);

                Assert.NotNull(view);
                Assert.IsType(viewType, view);
            }
        }

        public class can_add_and_remove_dependencies
        {
            public interface ITestDep
            {

            }

            [Fact]
            public void test()
            {
                var mockLogger = new Mock<ILogger>();
                var testObject = new Factory("test", mockLogger.Object);

                var mockDep = new Mock<ITestDep>();

                Assert.False(testObject.IsDependencyRegistered<ITestDep>());
                Assert.Null(testObject.RetreiveDependency<ITestDep>());

                testObject.Dep<ITestDep>(mockDep.Object);

                Assert.True(testObject.IsDependencyRegistered<ITestDep>());
                Assert.Equal(mockDep.Object, testObject.RetreiveDependency<ITestDep>());

                testObject.RemoveDep<ITestDep>();

                Assert.False(testObject.IsDependencyRegistered<ITestDep>());
                Assert.Null(testObject.RetreiveDependency<ITestDep>());
            }
        }

        [Fact]
        public void null_is_returned_when_dependency_isnt_found()
        {
            var mockLogger = new Mock<ILogger>();
            var testObject = new Factory("test", mockLogger.Object);

            var testName = "Blah";

            Assert.Null(testObject.RetreiveDependency(testName));
            Assert.False(testObject.IsDependencyRegistered(testName));
        }

        public class can_retreive_dependency_by_interface
        {
            public interface ITest
            {
            }

            [Fact]
            public void test()
            {
                var mockLogger = new Mock<ILogger>();
                var testObject = new Factory("test", mockLogger.Object);

                var testDep = new Mock<ITest>();

                testObject.Dep(typeof(ITest).Name, testDep.Object);

                Assert.Equal(testDep.Object, testObject.RetreiveDependency<ITest>());
                Assert.True(testObject.IsDependencyRegistered<ITest>());
            }
        }

        [Fact]
        public void can_retreive_dependency_from_fallback_provider()
        {
            var mockFallback = new Mock<IFactory>();
            var mockLogger = new Mock<ILogger>();
            var testObject = new Factory("test", mockFallback.Object, mockLogger.Object);

            var testName = "Blah";
            var testDep = new object();

            mockFallback
                .Setup(m => m.RetreiveDependency(testName))
                .Returns(testDep);
            mockFallback
                .Setup(m => m.IsDependencyRegistered(testName))
                .Returns(true);

            Assert.Equal(testDep, testObject.RetreiveDependency(testName));
            Assert.True(testObject.IsDependencyRegistered(testName));
        }

        [Fact]
        public void can_resolve_dependency_from_plugin()
        {
            var mockLogger = new Mock<ILogger>();
            var testObject = new Factory("test", mockLogger.Object);
            var testDep = new object();
            var requestedDepName = "Blah";
            var mockProvider = new Mock<IDependencyProvider>();
            mockProvider
                .Setup(m => m.ResolveDependency(requestedDepName))
                .Returns(testDep);

            testObject.AddDependencyProvider(mockProvider.Object);

            Assert.Equal(testDep, testObject.ResolveDep(requestedDepName));
        }

        [Fact]
        public void can_register_multiple_dependency_plugins()
        {
            var mockLogger = new Mock<ILogger>();
            var testObject = new Factory("test", mockLogger.Object);
            var testDep = new object();
            var requestedDepName = "Blah";
            var mockProvider1 = new Mock<IDependencyProvider>();
            mockProvider1
                .Setup(m => m.ResolveDependency(requestedDepName))
                .Returns(null);

            var mockProvider2 = new Mock<IDependencyProvider>();
            mockProvider2
                .Setup(m => m.ResolveDependency(requestedDepName))
                .Returns(testDep);

            testObject.AddDependencyProvider(mockProvider1.Object);
            testObject.AddDependencyProvider(mockProvider2.Object);

            Assert.Equal(testDep, testObject.ResolveDep(requestedDepName));
        }

        [Fact]
        public void can_resolve_dependency_from_plugin_in_fallback_factory()
        {
            var mockLogger = new Mock<ILogger>();
            var mockFallbackFactory = new Mock<IFactory>();
            var testObject = new Factory("test", mockFallbackFactory.Object, mockLogger.Object);
            var testDep = new object();
            var requestedDepName = "Blah";
            mockFallbackFactory
                .Setup(m => m.ResolveDependency(requestedDepName))
                .Returns(testDep);

            Assert.Equal(testDep, testObject.ResolveDep(requestedDepName));
        }

        public class can_resolve_constructor_dependency
        {
            public interface ITestDep
            {

            }

            public class Test
            {
                public ITestDep dep;

                public Test(ITestDep dep)
                {
                    this.dep = dep;
                }
            }

            [Fact]
            public void test()
            {
                var mockLogger = new Mock<ILogger>();

                var testObject = new Factory("test", mockLogger.Object);

                var mockDep = new Mock<ITestDep>();

                testObject.Dep<ITestDep>(mockDep.Object);

                var obj = (Test)testObject.Create(typeof(Test));

                Assert.Equal(mockDep.Object, obj.dep);
            }
        }

        public class can_resolve_constructor_dependency_from_fallback
        {
            public interface ITestDep
            {

            }

            public class Test
            {
                public ITestDep dep;

                public Test(ITestDep dep)
                {
                    this.dep = dep;
                }
            }

            [Fact]
            public void test()
            {
                var mockFallback = new Mock<IFactory>();
                var mockLogger = new Mock<ILogger>();

                var testObject = new Factory("test", mockFallback.Object, mockLogger.Object);

                var depName = typeof(ITestDep).Name;
                var mockDep = new Mock<ITestDep>();
                mockFallback
                    .Setup(m => m.RetreiveDependency(depName))
                    .Returns(mockDep.Object);
                mockFallback
                    .Setup(m => m.IsDependencyRegistered(depName))
                    .Returns(true);

                var obj = (Test)testObject.Create(typeof(Test));

                Assert.Equal(mockDep.Object, obj.dep);
            }
        }

        public class can_factory_create_constructor_dependency
        {
            public interface ITestDep
            {

            }

            public class TestDep : ITestDep
            {

            }

            public class Test
            {
                public ITestDep dep;

                public Test(ITestDep dep)
                {
                    this.dep = dep;
                }
            }

            [Fact]
            public void test()
            {
                var mockLogger = new Mock<ILogger>();

                var testObject = new Factory("test", mockLogger.Object);

                testObject.Type<ITestDep>(typeof(TestDep));

                var obj = (Test)testObject.Create(typeof(Test));

                Assert.NotNull(obj.dep);
                Assert.IsType(typeof(TestDep), obj.dep);
            }
        }

        public class can_resolve_property_dependency
        {
            public interface ITestDep
            {

            }

            public class Test
            {
                [Dependency]
                public ITestDep dep { get; set; }
            }

            [Fact]
            public void test()
            {
                var mockLogger = new Mock<ILogger>();

                var testObject = new Factory("test", mockLogger.Object);

                var mockDep = new Mock<ITestDep>();

                testObject.Dep<ITestDep>(mockDep.Object);

                var obj = (Test)testObject.Create(typeof(Test));

                Assert.Equal(mockDep.Object, obj.dep);
            }
        }

        public class can_resolve_property_dependency_twice
        {
            public interface ITestDep
            {

            }

            public class Test
            {
                [Dependency]
                public ITestDep dep { get; set; }
            }

            [Fact]
            public void test()
            {
                var mockLogger = new Mock<ILogger>();

                var testObject = new Factory("test", mockLogger.Object);

                var mockDep = new Mock<ITestDep>();

                testObject.Dep<ITestDep>(mockDep.Object);

                var obj = new Test();
                testObject.ResolveDependencies(obj);
                testObject.ResolveDependencies(obj);

                Assert.Equal(mockDep.Object, obj.dep);
            }
        }

        public class can_resolve_property_dependency_from_fallback
        {
            public interface ITestDep
            {

            }

            public class Test
            {
                [Dependency]
                public ITestDep dep { get; set; }
            }

            [Fact]
            public void test()
            {
                var mockFallback = new Mock<IFactory>();
                var mockLogger = new Mock<ILogger>();

                var testObject = new Factory("test", mockFallback.Object, mockLogger.Object);

                var mockDep = new Mock<ITestDep>();
                mockFallback
                    .Setup(m => m.RetreiveDependency(typeof(ITestDep).Name))
                    .Returns(mockDep.Object);

                var obj = (Test)testObject.Create(typeof(Test));

                Assert.Equal(mockDep.Object, obj.dep);
            }
        }

        public class can_factory_create_property_dependency
        {
            public interface ITestDep
            {

            }

            public class TestDep : ITestDep
            {

            }

            public class Test
            {
                [Dependency]
                public ITestDep dep { get; set; }
            }

            [Fact]
            public void test()
            {
                var mockLogger = new Mock<ILogger>();

                var testObject = new Factory("test", mockLogger.Object);

                testObject.Type<ITestDep>(typeof(TestDep));

                var obj = (Test)testObject.Create(typeof(Test));

                Assert.NotNull(obj.dep);
                Assert.IsType(typeof(TestDep), obj.dep);

            }
        }

        public class throws_exception_for_unresolved_constructor_dependency
        {
            public interface ITestDep
            {

            }

            public class Test
            {
                public ITestDep dep;

                public Test(ITestDep dep)
                {
                    this.dep = dep;
                }
            }

            [Fact]
            public void test()
            {
                var mockLogger = new Mock<ILogger>();

                var testObject = new Factory("test", mockLogger.Object);

                Assert.Throws<ApplicationException>(() =>
                    testObject.Create(typeof(Test))
                );
            }
        }

        public class throws_exception_for_unresolved_property_dependency
        {
            public interface ITestDep
            {

            }

            public class Test
            {
                [Dependency]
                public ITestDep dep { get; set; }
            }

            [Fact]
            public void test()
            {
                var mockLogger = new Mock<ILogger>();

                var testObject = new Factory("test", mockLogger.Object);

                Assert.Throws<ApplicationException>(() =>
                    testObject.Create(typeof(Test))
                );
            }
        }

        public class throws_exception_attempting_to_resolve_private_property_dependency
        {
            public interface ITestDep
            {

            }

            public class Test
            {
                [Dependency]
                private ITestDep dep { get; set; }
            }

            [Fact]
            public void test()
            {
                var mockLogger = new Mock<ILogger>();

                var testObject = new Factory("test", mockLogger.Object);

                Assert.Throws<ApplicationException>(() =>
                    testObject.Create(typeof(Test))
                );
            }
        }

        public class throws_exception_attempting_to_resolve_circular_property_dependency
        {
            public interface ITestDep1
            {

            }

            public class Test1 : ITestDep1
            {
                [Dependency]
                public ITestDep2 dep { get; set; }
            }

            public interface ITestDep2
            {

            }

            public class Test2 : ITestDep2
            {
                [Dependency]
                public ITestDep1 dep { get; set; }
            }

            [Fact]
            public void test()
            {
                var mockLogger = new Mock<ILogger>();

                var testObject = new Factory("test", mockLogger.Object);

                testObject.Type<ITestDep1>(typeof(Test1));
                testObject.Type<ITestDep2>(typeof(Test2));

                Assert.Throws<ApplicationException>(() =>
                    testObject.Create(typeof(Test1))
                );
            }
        }

        public class throws_exception_attempting_to_resolve_circular_constructor_dependency
        {
            public interface ITestDep1
            {

            }

            public class Test1 : ITestDep1
            {
                public Test1(ITestDep2 dep)
                {
                }
            }

            public interface ITestDep2
            {

            }

            public class Test2 : ITestDep2
            {
                public Test2(ITestDep1 dep)
                {
                }
            }

            [Fact]
            public void test()
            {
                var mockLogger = new Mock<ILogger>();

                var testObject = new Factory("test", mockLogger.Object);

                testObject.Type<ITestDep1>(typeof(Test1));
                testObject.Type<ITestDep2>(typeof(Test2));

                Assert.Throws<ApplicationException>(() =>
                    testObject.Create(typeof(Test1))
                );
            }
        }

        public class throws_exception_attempting_to_resolve_complex_circular_property_dependency
        {
            public interface ITestDep1
            {

            }

            public class Test1 : ITestDep1
            {
                [Dependency]
                public ITestDep2 dep { get; set; }
            }

            public interface ITestDep2
            {

            }

            public class Test2 : ITestDep2
            {
                [Dependency]
                public ITestDep3 dep { get; set; }
            }

            public interface ITestDep3
            {

            }

            public class Test3 : ITestDep3
            {
                [Dependency]
                public ITestDep1 dep { get; set; }
            }

            [Fact]
            public void test()
            {
                var mockLogger = new Mock<ILogger>();

                var testObject = new Factory("test", mockLogger.Object);

                testObject.Type<ITestDep1>(typeof(Test1));
                testObject.Type<ITestDep2>(typeof(Test2));
                testObject.Type<ITestDep3>(typeof(Test3));

                Assert.Throws<ApplicationException>(() =>
                    testObject.Create(typeof(Test1))
                );
            }
        }

        public class can_create_object_with_arguments
        {
            public class Test
            {
                public Test(string s, int a)
                {
                    this.s = s;
                    this.a = a;
                }

                public Test(int a, string s)
                {
                    this.s = s;
                    this.a = a;
                }

                public string s { get; set; }
                public int a { get; set; }
            }

            [Fact]
            public void test()
            {
                var mockLogger = new Mock<ILogger>();

                var testObject = new Factory("test", mockLogger.Object);

                var s = "foo";
                var a = 5;

                var obj = (Test)testObject.Create(typeof(Test), s, a);

                Assert.Equal(s, obj.s);
                Assert.Equal(a, obj.a);
            }
        }

        public class throws_exception_when_valid_constructor_cant_be_found
        {
            public class Test
            {
            }

            [Fact]
            public void test()
            {
                var mockLogger = new Mock<ILogger>();

                var testObject = new Factory("test", mockLogger.Object);

                var s = "foo";
                var a = 5;

                Assert.Throws<ApplicationException>(() =>
                    testObject.Create(typeof(Test), s, a)
                );
            }
        }

        public class can_create_object_with_arguments_and_dependency
        {
            public interface ITestDep
            {

            }

            public class Test
            {
                public Test(string s, int a, ITestDep dep)
                {
                    this.s = s;
                    this.a = a;
                    this.dep = dep;
                }

                public ITestDep dep;
                public string s { get; set; }
                public int a { get; set; }
            }

            [Fact]
            public void test()
            {
                var mockLogger = new Mock<ILogger>();

                var testObject = new Factory("test", mockLogger.Object);

                var mockDep = new Mock<ITestDep>();
                testObject.Dep<ITestDep>(mockDep.Object);

                var s = "foo";
                var a = 5;

                var obj = (Test)testObject.Create(typeof(Test), s, a);

                Assert.Equal(s, obj.s);
                Assert.Equal(a, obj.a);
                Assert.Equal(mockDep.Object, obj.dep);
            }
        }

        public class can_override_types
        {
            public class Test
            {
            }

            public class OverrideTest
            {
            }

            [Fact]
            public void test()
            {
                var mockLogger = new Mock<ILogger>();

                var typeName = "foo";

                var testObject = new Factory("test", mockLogger.Object);
                testObject.Type(typeName, typeof(Test));

                var overrideFactory = testObject.Override();
                overrideFactory.Type(typeName, typeof(OverrideTest));

                var obj = overrideFactory.Create<object>(typeName);

                Assert.NotNull(obj);
                Assert.IsType(typeof(OverrideTest), obj);
            }
        }

        public class can_override_dependencies
        {
            public interface IDep
            {
            }

            [Fact]
            public void test()
            {
                var mockLogger = new Mock<ILogger>();

                var mockDep = new Mock<IDep>();
                var mockOverrideDep = new Mock<IDep>();

                var testObject = new Factory("test", mockLogger.Object);
                testObject.Dep<IDep>(mockDep.Object);

                var overrideFactory = testObject.Override();
                overrideFactory.Dep<IDep>(mockOverrideDep.Object);

                var dep = overrideFactory.RetreiveDependency<IDep>();

                Assert.Equal(mockOverrideDep.Object, dep);
            }
        }

        public class constructor_resolution
        {
            public class test_can_resolve_default_constructor_when_none_defind
            {
                public class TestClass
                {
                }

                [Fact]
                public void test()
                {
                    var mockLogger = new Mock<ILogger>();
                    var testObject = new Factory("test", mockLogger.Object);

                    var testType = typeof(TestClass);
                    var argTypes = new Type[0];

                    Assert.Equal(testType.GetConstructor(argTypes), testObject.ResolveConstructor(testType, argTypes));
                }
            }

            public class test_can_resolve_default_constructor_when_there_is_an_explicit_constuctor
            {
                public class TestClass
                {
                    public TestClass()
                    {
                    }
                }

                [Fact]
                public void test()
                {
                    var mockLogger = new Mock<ILogger>();
                    var testObject = new Factory("test", mockLogger.Object);

                    var testType = typeof(TestClass);
                    var argTypes = new Type[0];

                    Assert.Equal(testType.GetConstructor(argTypes), testObject.ResolveConstructor(testType, argTypes));
                }
            }

            public class test_can_resolve_default_constructor_when_there_are_multiple_constructors
            {
                public class TestClass
                {
                    public TestClass()
                    {
                    }

                    public TestClass(int x)
                    {
                    }

                    public TestClass(string s)
                    {
                    }
                }

                [Fact]
                public void test()
                {
                    var mockLogger = new Mock<ILogger>();
                    var testObject = new Factory("test", mockLogger.Object);

                    var testType = typeof(TestClass);
                    var argTypes = new Type[0];

                    Assert.Equal(testType.GetConstructor(argTypes), testObject.ResolveConstructor(testType, argTypes));
                }
            }

            public class test_can_resolve_constructor_with_single_argument
            {
                public class TestClass
                {
                    public TestClass()
                    {
                    }

                    public TestClass(int x)
                    {
                    }

                    public TestClass(string s)
                    {
                    }
                }

                [Fact]
                public void test()
                {
                    var mockLogger = new Mock<ILogger>();
                    var testObject = new Factory("test", mockLogger.Object);

                    var testType = typeof(TestClass);
                    var argTypes = new Type[] { typeof(int) };

                    Assert.Equal(testType.GetConstructor(argTypes), testObject.ResolveConstructor(testType, argTypes));
                }
            }

            public class test_can_resolve_constructor_with_multiple_arguments
            {
                public class TestClass
                {
                    public TestClass()
                    {
                    }

                    public TestClass(int x, string s)
                    {
                    }

                    public TestClass(string s, int x)
                    {
                    }
                }

                [Fact]
                public void test()
                {
                    var mockLogger = new Mock<ILogger>();
                    var testObject = new Factory("test", mockLogger.Object);

                    var testType = typeof(TestClass);
                    var argTypes = new Type[] { typeof(int), typeof(string) };

                    Assert.Equal(testType.GetConstructor(argTypes), testObject.ResolveConstructor(testType, argTypes));
                }
            }

            public class test_can_resolve_constructor_with_object_parameter
            {
                public interface ITest
                {
                }

                public class TestClass
                {
                    public TestClass(object obj)
                    {
                    }
                }

                [Fact]
                public void test()
                {
                    var mockLogger = new Mock<ILogger>();
                    var testObject = new Factory("test", mockLogger.Object);

                    var testType = typeof(TestClass);
                    var argTypes = new Type[] { typeof(ITest) };

                    Assert.Equal(testType.GetConstructor(argTypes), testObject.ResolveConstructor(testType, argTypes));
                }
            }

            public class test_can_resolve_constructor_with_derived_class
            {
                public class BaseTest
                {
                }

                public class DerivedTest : BaseTest
                {
                }

                public class TestClass
                {
                    public TestClass(BaseTest obj)
                    {
                    }
                }

                [Fact]
                public void test()
                {
                    var mockLogger = new Mock<ILogger>();
                    var testObject = new Factory("test", mockLogger.Object);

                    var testType = typeof(TestClass);
                    var argTypes = new Type[] { typeof(DerivedTest) };

                    Assert.Equal(testType.GetConstructor(argTypes), testObject.ResolveConstructor(testType, argTypes));
                }
            }

            public class test_can_resolve_constructor_with_implementation_class
            {
                public interface ITest
                {
                }

                public class DerivedTest : ITest
                {
                }

                public class TestClass
                {
                    public TestClass(ITest obj)
                    {
                    }
                }

                [Fact]
                public void test()
                {
                    var mockLogger = new Mock<ILogger>();
                    var testObject = new Factory("test", mockLogger.Object);

                    var testType = typeof(TestClass);
                    var argTypes = new Type[] { typeof(DerivedTest) };

                    Assert.Equal(testType.GetConstructor(argTypes), testObject.ResolveConstructor(testType, argTypes));
                }
            }

            public class test_can_resolve_constructor_when_parameter_is_object_and_argument_is_null
            {
                public class TestClass
                {
                    public TestClass(object obj)
                    {
                    }
                }

                [Fact]
                public void test()
                {
                    var mockLogger = new Mock<ILogger>();
                    var testObject = new Factory("test", mockLogger.Object);

                    var testType = typeof(TestClass);
                    var argTypes = new Type[] { null };
                    var resolvedArgTypes = new Type[] { typeof(object) };

                    Assert.Equal(testType.GetConstructor(resolvedArgTypes), testObject.ResolveConstructor(testType, argTypes));
                }
            }

            public class test_can_resolve_constructor_when_parameter_is_class_and_argument_is_null
            {
                public class TestDep
                {
                }

                public class TestClass
                {
                    public TestClass(TestDep obj)
                    {
                    }
                }

                [Fact]
                public void test()
                {
                    var mockLogger = new Mock<ILogger>();
                    var testObject = new Factory("test", mockLogger.Object);

                    var testType = typeof(TestClass);
                    var argTypes = new Type[] { null };
                    var resolvedArgTypes = new Type[] { typeof(TestDep) };

                    Assert.Equal(testType.GetConstructor(resolvedArgTypes), testObject.ResolveConstructor(testType, argTypes));
                }
            }

            public class test_can_resolve_constructor_when_parameter_is_interface_and_argument_is_null
            {
                public interface ITestDep
                {
                }

                public class TestClass
                {
                    public TestClass(ITestDep obj)
                    {
                    }
                }

                [Fact]
                public void test()
                {
                    var mockLogger = new Mock<ILogger>();
                    var testObject = new Factory("test", mockLogger.Object);

                    var testType = typeof(TestClass);
                    var argTypes = new Type[] { null };
                    var resolvedArgTypes = new Type[] { typeof(ITestDep) };

                    Assert.Equal(testType.GetConstructor(resolvedArgTypes), testObject.ResolveConstructor(testType, argTypes));
                }
            }

            public class test_request_for_non_existing_constructor_throws_exception
            {
                public class TestClass
                {
                    public TestClass()
                    {
                    }

                    public TestClass(int x, string s)
                    {
                    }

                    public TestClass(string s, int x)
                    {
                    }
                }

                [Fact]
                public void test()
                {
                    var mockLogger = new Mock<ILogger>();
                    var testObject = new Factory("test", mockLogger.Object);

                    var testType = typeof(TestClass);
                    var argTypes = new Type[] { typeof(int), typeof(int) };

                    Assert.Throws<ApplicationException>(() =>
                        testObject.ResolveConstructor(testType, argTypes)
                    );
                }
            }

            public class test_can_resolve_constructor_with_no_arguments_and_single_dependency
            {
                public interface ITestDep1
                {
                }

                public class TestClass
                {
                    public TestClass(int x, string s)
                    {
                    }

                    public TestClass(string s, int x)
                    {
                    }

                    public TestClass(ITestDep1 testDep1)
                    {
                    }
                }

                [Fact]
                public void test()
                {
                    var mockLogger = new Mock<ILogger>();
                    var testObject = new Factory("test", mockLogger.Object);

                    testObject.Dep<ITestDep1>(new Mock<ITestDep1>().Object);

                    var testType = typeof(TestClass);
                    var resolvedArgTypes = new Type[] { typeof(ITestDep1) };

                    Assert.Equal(testType.GetConstructor(resolvedArgTypes), testObject.ResolveConstructor(testType, new Type[0]));
                }
            }

            public class test_can_resolve_constructor_with_no_arguments_and_multiple_dependencies
            {
                public interface ITestDep1
                {
                }

                public interface ITestDep2
                {
                }

                public class TestClass
                {
                    public TestClass(int x, string s)
                    {
                    }

                    public TestClass(string s, int x)
                    {
                    }

                    public TestClass(ITestDep1 testDep1, ITestDep2 testDep2)
                    {
                    }
                }

                [Fact]
                public void test()
                {
                    var mockLogger = new Mock<ILogger>();
                    var testObject = new Factory("test", mockLogger.Object);

                    testObject.Dep<ITestDep1>(new Mock<ITestDep1>().Object);
                    testObject.Dep<ITestDep2>(new Mock<ITestDep2>().Object);

                    var testType = typeof(TestClass);
                    var resolvedArgTypes = new Type[] { typeof(ITestDep1), typeof(ITestDep2) };

                    Assert.Equal(testType.GetConstructor(resolvedArgTypes), testObject.ResolveConstructor(testType, new Type[0]));
                }
            }

            public class test_can_resolve_constructor_with_arguments_and_multiple_dependencies
            {
                public interface ITestDep1
                {
                }

                public interface ITestDep2
                {
                }

                public class TestClass
                {
                    public TestClass(int x, string s)
                    {
                    }

                    public TestClass(string s, int x)
                    {
                    }

                    public TestClass(int x, int y, ITestDep1 testDep1, ITestDep2 testDep2)
                    {
                    }
                }

                [Fact]
                public void test()
                {
                    var mockLogger = new Mock<ILogger>();
                    var testObject = new Factory("test", mockLogger.Object);

                    testObject.Dep<ITestDep1>(new Mock<ITestDep1>().Object);
                    testObject.Dep<ITestDep2>(new Mock<ITestDep2>().Object);

                    var testType = typeof(TestClass);
                    var resolvedArgTypes = new Type[] { typeof(int), typeof(int), typeof(ITestDep1), typeof(ITestDep2) };
                    var regularArgTypes = new Type[] { typeof(int), typeof(int) };

                    Assert.Equal(testType.GetConstructor(resolvedArgTypes), testObject.ResolveConstructor(testType, regularArgTypes));
                }
            }

            public class test_can_exception_is_thrown_when_there_is_are_multiple_constructors_that_pass1
            {
                public interface ITestDep1
                {
                }

                public class TestClass
                {
                    public TestClass()
                    {
                    }

                    public TestClass(ITestDep1 testDep1)
                    {
                    }
                }

                [Fact]
                public void test()
                {
                    var mockLogger = new Mock<ILogger>();
                    var testObject = new Factory("test", mockLogger.Object);

                    testObject.Dep<ITestDep1>(new Mock<ITestDep1>().Object);

                    var testType = typeof(TestClass);
                    var regularArgTypes = new Type[] { };

                    Assert.Throws<ApplicationException>(() =>
                        testObject.ResolveConstructor(testType, regularArgTypes)
                    );
                }
            }

            public class test_exception_is_thrown_when_there_is_are_multiple_constructors_that_pass1
            {
                public interface ITestDep1
                {
                }

                public class TestClass
                {
                    public TestClass(int x)
                    {
                    }

                    public TestClass(int x, ITestDep1 testDep1)
                    {
                    }
                }

                [Fact]
                public void test()
                {
                    var mockLogger = new Mock<ILogger>();
                    var testObject = new Factory("test", mockLogger.Object);

                    testObject.Dep<ITestDep1>(new Mock<ITestDep1>().Object);

                    var testType = typeof(TestClass);
                    var regularArgTypes = new Type[] { typeof(int) };

                    Assert.Throws<ApplicationException>(() =>
                        testObject.ResolveConstructor(testType, regularArgTypes)
                    );
                }
            }

            public class test_exception_is_thrown_when_there_is_are_multiple_constructors_that_pass2
            {
                public interface ITestDep1
                {
                }

                public class TestClass
                {
                    public TestClass()
                    {
                    }

                    public TestClass(ITestDep1 testDep1)
                    {
                    }
                }

                [Fact]
                public void test()
                {
                    var mockLogger = new Mock<ILogger>();
                    var testObject = new Factory("test", mockLogger.Object);

                    testObject.Dep<ITestDep1>(new Mock<ITestDep1>().Object);

                    var testType = typeof(TestClass);
                    var regularArgTypes = new Type[] { };

                    Assert.Throws<ApplicationException>(() =>
                        testObject.ResolveConstructor(testType, regularArgTypes)
                    );
                }
            }

            public class test_exception_is_thrown_when_there_is_are_multiple_constructors_that_pass3
            {
                public interface ITestDep1
                {
                }

                public interface ITestDep2
                {
                }

                public class TestClass
                {
                    public TestClass(ITestDep1 testDep1)
                    {
                    }

                    public TestClass(ITestDep2 testDep2)
                    {
                    }
                }

                [Fact]
                public void test()
                {
                    var mockLogger = new Mock<ILogger>();
                    var testObject = new Factory("test", mockLogger.Object);

                    testObject.Dep<ITestDep1>(new Mock<ITestDep1>().Object);
                    testObject.Dep<ITestDep2>(new Mock<ITestDep2>().Object);

                    var testType = typeof(TestClass);
                    var regularArgTypes = new Type[] { };

                    Assert.Throws<ApplicationException>(() =>
                        testObject.ResolveConstructor(testType, regularArgTypes)
                    );
                }
            }

            public class test_exception_is_thrown_when_there_is_requested_injectable_is_not_availble
            {
                public interface ITestDep1
                {
                }

                public class TestClass
                {
                    public TestClass(ITestDep1 testDep1)
                    {
                    }
                }

                [Fact]
                public void test()
                {
                    var mockLogger = new Mock<ILogger>();
                    var testObject = new Factory("test", mockLogger.Object);

                    var testType = typeof(TestClass);
                    var regularArgTypes = new Type[] { };

                    Assert.Throws<ApplicationException>(() =>
                        testObject.ResolveConstructor(testType, regularArgTypes)
                    );
                }
            }
        }
   }
}