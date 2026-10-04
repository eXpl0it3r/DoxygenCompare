using CommandLine;
using DoxygenCompare.Reporting;

namespace DoxygenCompare;

public class Options
{
    [Option('a', "fileA", Required = true, HelpText = "Doxygen XML output of the old version, either the index.xml file or the directory containing it")]
    public string FileA { get; set; } = string.Empty;

    [Option('b', "fileB", Required = true, HelpText = "Doxygen XML output of the new version, either the index.xml file or the directory containing it")]
    public string FileB { get; set; } = string.Empty;

    [Option("nameA", HelpText = "Name of the old version used in the output, defaults to the path")]
    public string? NameA { get; set; }

    [Option("nameB", HelpText = "Name of the new version used in the output, defaults to the path")]
    public string? NameB { get; set; }

    [Option('f', "format", Default = OutputFormat.Text, HelpText = "Output format: Text, Markdown or Json")]
    public OutputFormat Format { get; set; }

    [Option('o', "output", HelpText = "Write the result to this file instead of the console")]
    public string? Output { get; set; }

    [Option('e', "exclude", Separator = ',', HelpText = "Comma separated scopes or names to ignore, e.g. sf::priv")]
    public IEnumerable<string> Exclude { get; set; } = [];

    [Option("excludeProtected", HelpText = "Ignore protected members")]
    public bool ExcludeProtected { get; set; }
}
