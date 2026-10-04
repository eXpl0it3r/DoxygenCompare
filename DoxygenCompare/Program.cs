using CommandLine;
using DoxygenCompare;
using DoxygenCompare.Api;
using DoxygenCompare.Comparison;
using DoxygenCompare.Reporting;

var parser = new Parser(settings =>
{
    settings.CaseInsensitiveEnumValues = true;
    settings.HelpWriter = Console.Error;
});

return parser.ParseArguments<Options>(args)
             .MapResult(Run, _ => 1);

static int Run(Options options)
{
    var surfaceOptions = new ApiSurfaceOptions
    {
        IncludeProtected = !options.ExcludeProtected,
        ExcludedNames = options.Exclude.ToList()
    };

    ApiSurface oldApi;
    ApiSurface newApi;

    try
    {
        oldApi = ApiSurface.Load(options.FileA, surfaceOptions);
        newApi = ApiSurface.Load(options.FileB, surfaceOptions);
    }
    catch (FileNotFoundException exception)
    {
        Console.Error.WriteLine(exception.Message);
        return 1;
    }

    var result = ApiComparer.Compare(oldApi, newApi, options.NameA ?? options.FileA, options.NameB ?? options.FileB);

    if (options.Output is null)
    {
        Reporter.Write(result, options.Format, Console.Out);
    }
    else
    {
        using var writer = new StreamWriter(options.Output);
        Reporter.Write(result, options.Format, writer);
    }

    return 0;
}
