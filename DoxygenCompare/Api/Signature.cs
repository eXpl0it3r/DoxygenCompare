using System.Text.RegularExpressions;

namespace DoxygenCompare.Api;

/// <summary>
/// Helpers to turn Doxygen's declaration fragments into comparable strings.
/// </summary>
public static partial class Signature
{
    /// <summary>
    /// Collapses whitespace and removes the spaces Doxygen puts around punctuation,
    /// so <c>const Texture &amp;</c> and <c>const Texture&amp;</c> compare equal.
    /// </summary>
    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var result = WhitespaceRegex().Replace(text, " ").Trim();
        result = PunctuationRegex().Replace(result, "$1");
        result = SpaceBeforeReferenceRegex().Replace(result, "$1");
        result = ReferenceBeforeNameRegex().Replace(result, "$1 ");
        result = AssignmentRegex().Replace(result, " = ");
        return result.Replace(",", ", ").Trim();
    }

    /// <summary>
    /// Splits a Doxygen argsstring like <c>(int a, int b=0) const noexcept</c> into the
    /// parameter list and the trailing qualifiers.
    /// </summary>
    public static (string Parameters, string Tail) SplitArguments(string? argsString)
    {
        var args = argsString ?? string.Empty;
        var start = args.IndexOf('(');

        if (start < 0)
        {
            return (string.Empty, Normalize(args));
        }

        var depth = 0;

        for (var i = start; i < args.Length; ++i)
        {
            if (args[i] == '(')
            {
                ++depth;
            }
            else if (args[i] == ')' && --depth == 0)
            {
                return (args[start..(i + 1)], Normalize(args[(i + 1)..]));
            }
        }

        return (args[start..], string.Empty);
    }

    /// <summary>
    /// Qualifiers that take part in overload resolution and as such belong into the key.
    /// </summary>
    public static string OverloadQualifiers(string tail)
    {
        var qualifiers = QualifierRegex().Matches(tail)
                                         .Select(m => m.Value)
                                         .Where(q => q is "const" or "volatile" or "&" or "&&");
        return string.Join(" ", qualifiers);
    }

    public static bool HasToken(string tail, string token) => QualifierRegex().Matches(tail).Any(m => m.Value == token);

    /// <summary>
    /// Returns the noexcept specification from the tail, e.g. <c>noexcept</c> or <c>noexcept(false)</c>.
    /// </summary>
    public static string? NoexceptSpecification(string tail)
    {
        var match = NoexceptRegex().Match(tail);
        return match.Success ? match.Value : null;
    }

    /// <summary>
    /// Returns the part behind <c>=</c> at the end of the tail, e.g. <c>delete</c>, <c>default</c> or <c>0</c>.
    /// </summary>
    public static string? PureOrDefinition(string tail)
    {
        var match = DefinitionRegex().Match(tail);
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>
    /// Returns the trailing return type, e.g. <c>-&gt; int</c>.
    /// </summary>
    public static string? TrailingReturnType(string tail)
    {
        var index = tail.IndexOf("->", StringComparison.Ordinal);
        return index < 0 ? null : tail[index..];
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"\s*([,()<>\[\]])\s*")]
    private static partial Regex PunctuationRegex();

    [GeneratedRegex(@"\s+([&*])")]
    private static partial Regex SpaceBeforeReferenceRegex();

    [GeneratedRegex(@"([&*])(?=[A-Za-z_])")]
    private static partial Regex ReferenceBeforeNameRegex();

    // Only plain assignments, not comparisons like == or <=
    [GeneratedRegex(@"\s*(?<![=!<>])=(?!=)\s*")]
    private static partial Regex AssignmentRegex();

    [GeneratedRegex(@"&&|&|->.*|noexcept(\([^)]*\))?|=\s*\w+|\w+")]
    private static partial Regex QualifierRegex();

    [GeneratedRegex(@"noexcept(\([^)]*\))?")]
    private static partial Regex NoexceptRegex();

    [GeneratedRegex(@"=\s*(\w+)\s*$")]
    private static partial Regex DefinitionRegex();
}
