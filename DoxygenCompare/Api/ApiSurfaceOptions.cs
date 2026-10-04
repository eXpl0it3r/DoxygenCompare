namespace DoxygenCompare.Api;

public sealed record ApiSurfaceOptions
{
    /// <summary>
    /// Protected members are part of the API for derived classes, as such they're included by default.
    /// </summary>
    public bool IncludeProtected { get; init; } = true;

    /// <summary>
    /// Scopes or names to ignore, e.g. <c>sf::priv</c>, matched on whole <c>::</c> separated segments.
    /// </summary>
    public IReadOnlyList<string> ExcludedNames { get; init; } = [];
}
