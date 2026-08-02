namespace LeosTools.Runtime
{
    /// <summary>
    /// Represents a style class
    /// </summary>
    public class StyleEntry
    {
        public readonly string Class;

        public StyleEntry(string styleClass)
        {
            Class = styleClass;
        }

        public static implicit operator string(StyleEntry entry) => entry.Class;
    }
}
