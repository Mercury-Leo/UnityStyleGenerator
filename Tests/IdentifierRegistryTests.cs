using System.Collections.Generic;
using LeosTools.Editor;
using NUnit.Framework;

namespace StyleGenerator.Editor.Tests
{
    public class IdentifierRegistryTests
    {
        [Test]
        public void TryRegisterIdentifier_NewName_ReturnsTrue()
        {
            var seen = new HashSet<string>();
            Assert.That(Utility.TryRegisterIdentifier(seen, "Foo", "foo", "test"), Is.True);
        }

        [Test]
        public void TryRegisterIdentifier_DuplicateName_ReturnsFalseAndKeepsFirstOnly()
        {
            var seen = new HashSet<string>();
            Utility.TryRegisterIdentifier(seen, "Foo", "foo", "test");

            Assert.That(Utility.TryRegisterIdentifier(seen, "Foo", "foo-again", "test"), Is.False);
            Assert.That(seen, Has.Count.EqualTo(1));
        }

        [Test]
        public void TryRegisterIdentifier_NullOrEmptyName_ReturnsFalse()
        {
            var seen = new HashSet<string>();
            Assert.That(Utility.TryRegisterIdentifier(seen, null, "raw", "test"), Is.False);
            Assert.That(Utility.TryRegisterIdentifier(seen, string.Empty, "raw", "test"), Is.False);
            Assert.That(seen, Is.Empty);
        }
    }
}
