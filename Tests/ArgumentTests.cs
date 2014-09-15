using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utils.Dbg;
using Xunit;

namespace Utils.Tests.Dbg
{
    /* These tests don't work in release build.
     * 
    public class ArgumentTests
    {
        public static void FunctionThatNullChecksTheArgument(object argument)
        {
            Argument.NotNull(() => argument);
        }

        [Fact]
        public void test_no_exception_is_thrown_when_object_is_not_null()
        {
            // Expect no exception to be thrown.
            FunctionThatNullChecksTheArgument(new object());
        }

        private static void CheckArgumentNullException(string expectedParamName, string functionName, string typeName, ArgumentNullException ex)
        {
            Assert.Equal(expectedParamName, ex.ParamName);
            Assert.Contains(functionName, ex.Message);
            Assert.Contains(typeName, ex.Message);
        }

        [Fact]
        public void test_that_argument_null_exception_is_thrown_if_the_object_is_null()
        {
            var ex =
                Assert.Throws<ArgumentNullException>(() => 
                    FunctionThatNullChecksTheArgument(null)
                );

            CheckArgumentNullException("argument", "FunctionThatNullChecksTheArgument", "Object", ex);
        }
        
        /* This test is no longer necessary. 
         * It won't even compile after changes to Argument.NotNull.
        [Fact]
        public void test_that_argument_null_cant_be_used_with_constant_null()
        {
            Assert.Throws<ApplicationException>(() =>
                Argument.NotNull(() => null)
            );
        }
         

        [Fact]
        public void test_that_argument_null_cant_be_used_with_other_constant()
        {
            Assert.Throws<ApplicationException>(() =>
                Argument.NotNull(() => 5)
            );
        }

        public static void FunctionThatChecksStringArgument(string stringArgument)
        {
            Argument.StringNotNullOrEmpty(() => stringArgument);
        }

        private static void CheckInvalidStringException(ArgumentException ex)
        {
            Assert.Equal("stringArgument", ex.ParamName);
            Assert.Contains("FunctionThatChecksStringArgument", ex.Message);
            Assert.Contains("String", ex.Message);
        }

        [Fact]
        public void test_valid_string_passes_check()
        {
            // Expect no exception.
            FunctionThatChecksStringArgument("hello");
        }

        [Fact]
        public void test_null_string_throws_exception()
        {
            var ex =
                Assert.Throws<ArgumentNullException>(() =>
                    FunctionThatChecksStringArgument(null)
                );

            CheckArgumentNullException("stringArgument", "FunctionThatChecksStringArgument", "String", ex);
        }


        [Fact]
        public void test_empty_string_throws_exception()
        {
            var ex =
                Assert.Throws<ArgumentException>(() =>
                    FunctionThatChecksStringArgument("")
                );

            CheckInvalidStringException(ex);
        }

        [Fact]
        public void test_that_string_argument_check_cant_be_used_with_constant_null()
        {
            Assert.Throws<ApplicationException>(() =>
                Argument.StringNotNullOrEmpty(() => null)
            );
        }

        [Fact]
        public void test_that_string_argument_check_cant_be_used_with_other_constant()
        {
            Assert.Throws<ApplicationException>(() =>
                Argument.StringNotNullOrEmpty(() => "hello")
            );
        }

        void FunctionThatChecksIndexArgument(int arg)
        {
            Argument.ArrayIndex(() => arg);
        }

        [Fact]
        public void positive_index_argument_is_ok()
        {
            FunctionThatChecksIndexArgument(0);
            FunctionThatChecksIndexArgument(1);
        }

        [Fact]
        public void negative_index_throws_exception()
        {
            Assert.Throws<ArgumentException>(() =>
                FunctionThatChecksIndexArgument(-1)
            );
        }

        void FunctionThatChecksIndexArgument(int arg, int maxArrayElements)
        {
            Argument.ArrayIndex(() => arg, maxArrayElements);
        }

        [Fact]
        public void index_out_of_bounds_throws_exception()
        {
            Assert.Throws<ArgumentException>(() =>
                FunctionThatChecksIndexArgument(10, 3)
            );
        }
    }
    */
}
