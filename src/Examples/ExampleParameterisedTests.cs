using System;
using System.Collections.Generic;
using System.IO;
using Allure.Xunit.Attributes;
using Xunit;

namespace Examples
{
    public class ExampleParameterisedTests
    {
        public static IEnumerable<object[]> Data =>
            new List<object[]>
            {
                new object[] {1, 2, 3},
                new object[] {-4, -6, -10},
                new object[] {-2, 2, 0},
                new object[] {int.MinValue, -1, int.MaxValue},
            };

        public ExampleParameterisedTests()
        {
            Environment.CurrentDirectory = Path.GetDirectoryName(GetType().Assembly.Location);
        }

        [AllureXunitTheory]
        [AllureParentSuite("AllTests")]
        [AllureSuite("Test AllureXunitTheory")]
        [AllureSubSuite("Test MemberData")]
        [MemberData(nameof(Data))]
        public void TestTheoryWithMemberDataProperty(int value1, int value2, int expected)
        {
            var result = value1 + value2;

            Assert.Equal(expected, result);
        }

        [AllureXunitTheory]
        [AllureParentSuite("AllTests")]
        [AllureSuite("Test AllureXunitTheory")]
        [AllureSubSuite("Test ClassData")]
        [ClassData(typeof(TestClassData))]
        public void TestTheoryWithClassData(int value1, int value2, int expected)
        {
            var result = value1 + value2;

            Assert.Equal(expected, result);
        }

        [AllureXunitTheory]
        [AllureParentSuite("AllTests")]
        [AllureSuite("Test AllureXunitTheory")]
        [AllureSubSuite("Test InlineData")]
        [InlineData(1, 1)]
        [InlineData(1, 2)]
        public void TestTheory(int a, int b)
        {
            Assert.Equal(a, b);
        }
    }
}
