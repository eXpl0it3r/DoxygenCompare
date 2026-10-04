namespace DoxygenCompare.Api;

/// <summary>
/// A single element of the public API, e.g. a class, a function overload or an enum value.
/// </summary>
/// <param name="Key">Identity used to match the entity between two versions, overloads differ in their parameter types</param>
/// <param name="ParentKey">Key of the enclosing entity, empty for top-level entities</param>
/// <param name="Scope">Qualified name of the enclosing scope, used to group the output</param>
/// <param name="Signature">Human-readable declaration</param>
/// <param name="Properties">Everything that is compared between two versions, besides the key</param>
public sealed record ApiEntity(
    ApiKind Kind,
    string Key,
    string ParentKey,
    string Scope,
    string Name,
    string Signature,
    IReadOnlyDictionary<string, string> Properties,
    string Location)
{
    public bool IsDeprecated => Properties.TryGetValue(ApiProperties.Deprecated, out var value) && value == "yes";
}
