namespace DoxygenCompare.Comparison;

public enum ApiChangeKind
{
    Added,
    Removed,

    /// <summary>
    /// A function or friend whose only overload got replaced by one with a different signature.
    /// </summary>
    SignatureChanged,

    /// <summary>
    /// Same declaration, but e.g. a different return type, default argument or deprecation state.
    /// </summary>
    Changed
}
