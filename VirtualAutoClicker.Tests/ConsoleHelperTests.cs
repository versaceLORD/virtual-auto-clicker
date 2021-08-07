using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace VirtualAutoClicker.Tests
{
    [TestClass]
    public class ConsoleHelperTests
    {
        [TestMethod]
        public void GetInputArguments_QuotesNoSpaces_IsValid()
        {
            var arguments = new List<string>()
            {
                "\"chrome\"",
                "500,500",
                "50",
                "n",
            };

            var inputArguments = ConsoleHelper.GetInputArguments(arguments);

            Assert.AreEqual(inputArguments.Length, 4);
            Assert.AreEqual(inputArguments[0], "chrome");
            Assert.AreEqual(inputArguments[1], "500,500");
            Assert.AreEqual(inputArguments[2], "50");
            Assert.AreEqual(inputArguments[3], "n");
        }

        [TestMethod]
        public void GetInputArguments_QuotesWithSpaces_IsValid()
        {
            var arguments = new List<string>()
            {
                "\"Google Chrome\"",
                "500,500",
                "50",
                "n",
            };

            var inputArguments = ConsoleHelper.GetInputArguments(arguments);

            Assert.AreEqual(inputArguments.Length, 4);
            Assert.AreEqual(inputArguments[0], "Google Chrome");
            Assert.AreEqual(inputArguments[1], "500,500");
            Assert.AreEqual(inputArguments[2], "50");
            Assert.AreEqual(inputArguments[3], "n");
        }

        [TestMethod]
        public void GetInputArguments_NoQuotesWithoutSpaces_IsValid()
        {
            var arguments = new List<string>()
            {
                "chrome",
                "500,500",
                "50",
                "n",
            };

            var inputArguments = ConsoleHelper.GetInputArguments(arguments);

            Assert.AreEqual(inputArguments.Length, 4);
            Assert.AreEqual(inputArguments[0], "chrome");
            Assert.AreEqual(inputArguments[1], "500,500");
            Assert.AreEqual(inputArguments[2], "50");
            Assert.AreEqual(inputArguments[3], "n");
        }
    }
}
