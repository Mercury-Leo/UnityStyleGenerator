using System.Linq;
using LeosTools.Editor;
using NUnit.Framework;

namespace StyleGenerator.Editor.Tests
{
    public class SelectorTokenizerTests
    {
        [Test]
        public void ExtractClassNames_PlainSelector_ReturnsClass()
        {
            var result = Utility.ExtractClassNames(".plain { color: red; }");
            Assert.That(result, Does.Contain("plain"));
        }

        [Test]
        public void ExtractClassNames_PseudoClassSelector_ReturnsClass()
        {
            var result = Utility.ExtractClassNames(".foo:hover { color: red; }");
            Assert.That(result, Does.Contain("foo"));
        }

        [Test]
        public void ExtractClassNames_CompoundSelector_ReturnsBothClasses()
        {
            var result = Utility.ExtractClassNames(".foo.bar { color: red; }").ToList();
            Assert.That(result, Does.Contain("foo"));
            Assert.That(result, Does.Contain("bar"));
        }

        [Test]
        public void ExtractClassNames_CommaSeparatedGroup_ReturnsBothClasses()
        {
            var result = Utility.ExtractClassNames(".a, .b { color: red; }").ToList();
            Assert.That(result, Does.Contain("a"));
            Assert.That(result, Does.Contain("b"));
        }

        [Test]
        public void ExtractClassNames_ClassInsideComment_IsIgnored()
        {
            var result = Utility
                .ExtractClassNames("/* .commented { color: red; } */ .live { color: blue; }")
                .ToList();
            Assert.That(result, Does.Not.Contain("commented"));
            Assert.That(result, Does.Contain("live"));
        }

        [Test]
        public void ExtractClassNames_ImportStatementBeforeRule_DoesNotLeakIntoNextSelector()
        {
            var result = Utility.ExtractClassNames("@import url(\"theme.uss\");\n.foo { color: red; }").ToList();
            Assert.That(result, Does.Not.Contain("uss"));
            Assert.That(result, Does.Contain("foo"));
        }
    }
}
