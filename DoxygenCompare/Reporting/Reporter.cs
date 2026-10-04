using System.Text.Encodings.Web;
using System.Text.Json;
using DoxygenCompare.Api;
using DoxygenCompare.Comparison;

namespace DoxygenCompare.Reporting;

public static class Reporter
{
    // Signatures are full of quotes and angle brackets, which the default encoder would escape for HTML
    private static readonly JsonSerializerOptions JsonOptions = new(ReportJsonContext.Default.Options)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static void Write(ComparisonResult result, OutputFormat format, TextWriter writer)
    {
        switch (format)
        {
            case OutputFormat.Json:
                writer.WriteLine(JsonSerializer.Serialize(result, JsonOptions.GetTypeInfo(typeof(ComparisonResult))));
                break;
            case OutputFormat.Markdown:
                WriteMarkdown(result, writer);
                break;
            default:
                WriteText(result, writer);
                break;
        }
    }

    private static void WriteText(ComparisonResult result, TextWriter writer)
    {
        writer.WriteLine($"API changes from {result.OldVersion} to {result.NewVersion}");
        writer.WriteLine(Summary(result));

        foreach (var scope in result.Changes.GroupBy(c => c.Scope))
        {
            writer.WriteLine();
            writer.WriteLine($"{ScopeTitle(scope.Key)}:");

            foreach (var group in scope.GroupBy(c => c.Change))
            {
                writer.WriteLine($"- {Title(group.Key)}:");

                foreach (var change in group)
                {
                    if (change.Change == ApiChangeKind.SignatureChanged)
                    {
                        writer.WriteLine($"  - {KindName(change.Kind)} {change.OldSignature}");
                        writer.WriteLine($"    -> {change.NewSignature}");
                    }
                    else
                    {
                        writer.WriteLine($"  - {KindName(change.Kind)} {change.Signature}");
                    }

                    foreach (var property in change.Properties)
                    {
                        writer.WriteLine($"    - {property.Property}: {property.OldValue ?? "(none)"} -> {property.NewValue ?? "(none)"}");
                    }
                }
            }
        }
    }

    private static void WriteMarkdown(ComparisonResult result, TextWriter writer)
    {
        writer.WriteLine($"# API Changes from {result.OldVersion} to {result.NewVersion}");
        writer.WriteLine();
        writer.WriteLine(Summary(result));

        foreach (var scope in result.Changes.GroupBy(c => c.Scope))
        {
            writer.WriteLine();
            writer.WriteLine($"## {ScopeTitle(scope.Key)}");

            foreach (var group in scope.GroupBy(c => c.Change))
            {
                writer.WriteLine();
                writer.WriteLine($"### {Title(group.Key)}");
                writer.WriteLine();

                foreach (var change in group)
                {
                    if (change.Change == ApiChangeKind.SignatureChanged)
                    {
                        writer.WriteLine($"- {KindName(change.Kind)} `{change.OldSignature}`  ");
                        writer.WriteLine($"  → `{change.NewSignature}`");
                    }
                    else
                    {
                        writer.WriteLine($"- {KindName(change.Kind)} `{change.Signature}`");
                    }

                    foreach (var property in change.Properties)
                    {
                        writer.WriteLine($"  - {property.Property}: {Code(property.OldValue)} → {Code(property.NewValue)}");
                    }
                }
            }
        }
    }

    private static string Summary(ComparisonResult result) =>
        $"{result.Count(ApiChangeKind.Added)} added, {result.Count(ApiChangeKind.Removed)} removed, " +
        $"{result.Count(ApiChangeKind.SignatureChanged)} signatures changed, {result.Count(ApiChangeKind.Changed)} changed";

    private static string ScopeTitle(string scope) => scope.Length == 0 ? "Global Scope" : scope;

    private static string Title(ApiChangeKind kind) => kind switch
    {
        ApiChangeKind.Added => "Added",
        ApiChangeKind.Removed => "Removed",
        ApiChangeKind.SignatureChanged => "Signature Changed",
        _ => "Changed"
    };

    private static string KindName(ApiKind kind) => kind switch
    {
        ApiKind.EnumValue => "enum value",
        _ => kind.ToString().ToLowerInvariant()
    };

    private static string Code(string? value) => value is null ? "(none)" : $"`{value}`";
}
