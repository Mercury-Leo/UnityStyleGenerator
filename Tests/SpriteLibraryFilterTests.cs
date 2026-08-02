using LeosTools.Editor;
using NUnit.Framework;

namespace StyleGenerator.Editor.Tests
{
    public class SpriteLibraryFilterTests
    {
        [Test]
        public void MatchesSearch_EmptySearch_MatchesEverything()
        {
            Assert.That(Utility.MatchesSearch(string.Empty, "Buttons", "Primary"), Is.True);
        }

        [Test]
        public void MatchesSearch_GroupNameSubstring_Matches()
        {
            Assert.That(Utility.MatchesSearch("butt", "Buttons", "Primary"), Is.True);
        }

        [Test]
        public void MatchesSearch_EntryNameSubstring_Matches()
        {
            Assert.That(Utility.MatchesSearch("prim", "Buttons", "Primary"), Is.True);
        }

        [Test]
        public void MatchesSearch_CaseInsensitive_Matches()
        {
            Assert.That(Utility.MatchesSearch("BUTTONS", "Buttons", "Primary"), Is.True);
        }

        [Test]
        public void MatchesSearch_NoSubstringMatch_ReturnsFalse()
        {
            Assert.That(Utility.MatchesSearch("cursor", "Buttons", "Primary"), Is.False);
        }
    }
}
