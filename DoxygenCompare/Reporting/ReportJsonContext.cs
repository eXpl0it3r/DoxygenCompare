using System.Text.Json.Serialization;
using DoxygenCompare.Comparison;

namespace DoxygenCompare.Reporting;

// Source generated, because the published executable is trimmed and reflection-based serialization is disabled there
[JsonSourceGenerationOptions(WriteIndented = true,
                             PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
                             DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                             UseStringEnumConverter = true)]
[JsonSerializable(typeof(ComparisonResult))]
internal partial class ReportJsonContext : JsonSerializerContext;
