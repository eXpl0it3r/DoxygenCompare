using DoxygenCompare.Api;

namespace DoxygenCompare.Comparison;

public sealed record ApiChange(
    ApiChangeKind Change,
    ApiKind Kind,
    string Scope,
    string Name,
    string? OldSignature,
    string? NewSignature,
    IReadOnlyList<PropertyChange> Properties,
    string Location)
{
    public string Signature => NewSignature ?? OldSignature ?? Name;
}
