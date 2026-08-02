using LeosTools.Editor;
using NUnit.Framework;

namespace StyleGenerator.Editor.Tests
{
    public class SanitizeNameTests
    {
        [Test]
        public void SanitizeName_HyphensAndSpaces_CamelCasesWithoutLeadingCapital()
        {
            Assert.That(Utility.SanitizeName("foo-bar baz"), Is.EqualTo("fooBarBaz"));
        }

        [Test]
        public void SanitizeName_CapitalizeFirst_CapitalizesLeadingChar()
        {
            Assert.That(Utility.SanitizeName("foo-bar baz", capitalizeFirst: true), Is.EqualTo("FooBarBaz"));
        }

        [Test]
        public void SanitizeName_KeepHyphens_PreservesThemLiterally()
        {
            Assert.That(Utility.SanitizeName("foo-bar", keepHyphens: true), Is.EqualTo("foo-bar"));
        }

        [Test]
        public void SanitizeName_SymbolsOnly_ReturnsEmptyNotNull()
        {
            Assert.That(Utility.SanitizeName("!!!", keepHyphens: true), Is.Empty);
        }
    }
}
