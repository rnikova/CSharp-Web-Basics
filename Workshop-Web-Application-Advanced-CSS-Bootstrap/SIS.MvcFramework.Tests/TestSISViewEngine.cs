﻿using Xunit;
using System.IO;
using SIS.MvcFramework.ViewEngine;
using System.Collections.Generic;
using SIS.MvcFramework.Validation;

namespace SIS.MvcFramework.Tests
{
    public class TestSISViewEngine
    {
        [Theory]
        [InlineData("TestWithoutCSharpCode")]
        [InlineData("UseForForeachAndIf")]
        [InlineData("UseModelData")]
        public void TestGetHtml(string testFileName)
        {
            IViewEngine viewEngine = new SISViewEngine();
            var viewFileName = $"ViewTests/{testFileName}.html";
            var expectedResultFileName = $"ViewTests/{testFileName}.Result.html";

            var viewContent = File.ReadAllText(viewFileName);
            var expectedResult = File.ReadAllText(expectedResultFileName);

            var actualResult = viewEngine.GetHtml<object>(viewContent, new TestViewModel()
            {
                StringValue = "str",
                ListValues = new List<string> { "123", "val1", string.Empty },
            },
            new ModelStateDictionary(),
            new Identity.Principal() { });

            Assert.Equal(expectedResult.TrimEnd(), actualResult.TrimEnd());
        }
    }
}
