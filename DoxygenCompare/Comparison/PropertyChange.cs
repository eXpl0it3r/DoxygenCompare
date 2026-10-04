namespace DoxygenCompare.Comparison;

/// <summary>
/// A property that differs between two versions, a missing property is <c>null</c>.
/// </summary>
public sealed record PropertyChange(string Property, string? OldValue, string? NewValue);
