using CommandLine;

namespace DoxygenCompare;

public class Options
{
    [Option('a', "fileA", Required = true, HelpText = "First doxygen index.xml file for comparison")]
    public string FileA { get; set; }

    [Option('b', "fileB", Required = true, HelpText = "Second doxygen index.xml file for comparison")]
    public string FileB { get; set; }
}