using CommandLine;
using DoxygenCompare;

Parser.Default.ParseArguments<Options>(args)
      .WithParsed(RunOptions);

void RunOptions(Options options)
{
    if (!File.Exists(options.FileA))
    {
        Console.WriteLine($"Invalid file provided: {options.FileA}");
        return;
    }
    
    if (!File.Exists(options.FileB))
    {
        Console.WriteLine($"Invalid file provided: {options.FileB}");
        return;
    }

    var doxygenIndex1 = new DoxygenIndex(options.FileA);
    var doxygenIndex2 = new DoxygenIndex(options.FileB);

    doxygenIndex1.Compare(doxygenIndex2);
}