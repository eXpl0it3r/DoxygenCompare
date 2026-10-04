namespace DoxygenCompare.Comparison;

public sealed record ComparisonResult(string OldVersion, string NewVersion, IReadOnlyList<ApiChange> Changes)
{
    public int Count(ApiChangeKind kind) => Changes.Count(c => c.Change == kind);
}
