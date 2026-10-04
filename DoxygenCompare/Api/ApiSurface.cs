using System.Xml.Linq;

namespace DoxygenCompare.Api;

/// <summary>
/// The public API of one library version, read from Doxygen's XML output.
/// </summary>
public sealed class ApiSurface
{
    private static readonly string[] ScopeCompoundKinds = ["namespace", "class", "struct", "union"];

    private readonly Dictionary<string, ApiEntity> _entities = new(StringComparer.Ordinal);
    private readonly HashSet<string> _seenMemberIds = new(StringComparer.Ordinal);
    private readonly ApiSurfaceOptions _options;
    private readonly string _directory;

    private ApiSurface(string directory, ApiSurfaceOptions options)
    {
        _directory = directory;
        _options = options;
    }

    public IReadOnlyDictionary<string, ApiEntity> Entities => _entities;

    /// <summary>
    /// Loads the API from Doxygen's XML output.
    /// </summary>
    /// <param name="path">Either the <c>index.xml</c> file or the directory containing it</param>
    /// <param name="options">Filter options</param>
    public static ApiSurface Load(string path, ApiSurfaceOptions? options = null)
    {
        var indexFile = Directory.Exists(path) ? Path.Combine(path, "index.xml") : path;

        if (!File.Exists(indexFile))
        {
            throw new FileNotFoundException($"Doxygen index file not found: {indexFile}", indexFile);
        }

        var surface = new ApiSurface(Path.GetDirectoryName(Path.GetFullPath(indexFile))!, options ?? new ApiSurfaceOptions());
        surface.Read(XDocument.Load(indexFile));
        return surface;
    }

    private void Read(XDocument index)
    {
        var compounds = index.Root?.Elements("compound").ToList() ?? [];

        // Members show up in several compounds, e.g. related functions in their class and their namespace,
        // so the scopes go first and files only contribute macros and global declarations
        foreach (var compound in compounds.Where(c => ScopeCompoundKinds.Contains((string?)c.Attribute("kind"))))
        {
            ReadCompound((string)compound.Attribute("refid")!);
        }

        foreach (var compound in compounds.Where(c => (string?)c.Attribute("kind") == "file"))
        {
            ReadCompound((string)compound.Attribute("refid")!);
        }
    }

    private void ReadCompound(string refId)
    {
        var file = Path.Combine(_directory, refId + ".xml");

        if (!File.Exists(file))
        {
            return;
        }

        foreach (var definition in XDocument.Load(file).Root?.Elements("compounddef") ?? [])
        {
            var kind = (string?)definition.Attribute("kind");
            var name = Signature.Normalize(definition.Element("compoundname")?.Value);

            if (kind == "file")
            {
                ReadMembers(definition, scope: string.Empty, parentKey: string.Empty);
                continue;
            }

            if (!IsIncluded((string?)definition.Attribute("prot")) || IsExcluded(name))
            {
                continue;
            }

            var entityKind = kind switch
            {
                "namespace" => ApiKind.Namespace,
                "struct" => ApiKind.Struct,
                "union" => ApiKind.Union,
                _ => ApiKind.Class
            };

            var (scope, shortName) = SplitQualifiedName(name);
            var properties = new SortedDictionary<string, string>
            {
                [ApiProperties.Kind] = kind ?? string.Empty,
                [ApiProperties.Deprecated] = IsDeprecated(definition) ? "yes" : "no"
            };

            if (entityKind != ApiKind.Namespace)
            {
                properties[ApiProperties.Protection] = (string?)definition.Attribute("prot") ?? "public";
                properties[ApiProperties.Abstract] = (string?)definition.Attribute("abstract") ?? "no";
                properties[ApiProperties.Final] = (string?)definition.Attribute("final") ?? "no";
                AddIfNotEmpty(properties, ApiProperties.TemplateParameters, TemplateParameters(definition));
                AddIfNotEmpty(properties, ApiProperties.BaseClasses, BaseClasses(definition));
            }

            var templatePrefix = properties.TryGetValue(ApiProperties.TemplateParameters, out var templateParameters)
                ? $"template<{templateParameters}> "
                : string.Empty;

            Add(new ApiEntity(entityKind,
                              name,
                              scope,
                              scope,
                              shortName,
                              $"{templatePrefix}{kind} {name}",
                              properties,
                              Location(definition)));

            ReadMembers(definition, scope: name, parentKey: name);
        }
    }

    private void ReadMembers(XElement compound, string scope, string parentKey)
    {
        foreach (var member in compound.Elements("sectiondef").Elements("memberdef"))
        {
            var id = (string?)member.Attribute("id") ?? string.Empty;

            if (!_seenMemberIds.Add(id) || !IsIncluded((string?)member.Attribute("prot")))
            {
                continue;
            }

            var name = member.Element("name")?.Value ?? string.Empty;
            var qualifiedName = member.Element("qualifiedname")?.Value ?? name;

            // Unnamed members, e.g. anonymous unions, can't be matched between versions
            if (string.IsNullOrEmpty(name) || name.StartsWith('@') || IsExcluded(qualifiedName))
            {
                continue;
            }

            switch ((string?)member.Attribute("kind"))
            {
                case "function":
                    Add(ReadFunction(member, ApiKind.Function, scope, parentKey, name));
                    break;
                case "friend":
                    Add(ReadFriend(member, scope, parentKey, name));
                    break;
                case "variable":
                    Add(ReadVariable(member, scope, parentKey, name));
                    break;
                case "typedef":
                    Add(ReadTypedef(member, scope, parentKey, name));
                    break;
                case "enum":
                    ReadEnum(member, scope, parentKey, name);
                    break;
                case "define":
                    Add(ReadMacro(member, name));
                    break;
            }
        }
    }

    private ApiEntity ReadFunction(XElement member, ApiKind kind, string scope, string parentKey, string name)
    {
        var properties = CommonProperties(member);
        var parameters = member.Elements("param").ToList();
        var (_, tail) = Signature.SplitArguments(member.Element("argsstring")?.Value);
        var parameterTypes = string.Join(", ", parameters.Select(ParameterType));
        var qualifiers = Signature.OverloadQualifiers(tail);
        var templateParameters = TemplateParameters(member);
        var templatePrefix = templateParameters.Length > 0 ? $"template<{templateParameters}> " : string.Empty;
        var returnType = Signature.Normalize(member.Element("type")?.Value);
        var trailingReturnType = Signature.TrailingReturnType(tail);

        properties[ApiProperties.Type] = trailingReturnType is null ? returnType : $"{returnType} {trailingReturnType}";
        properties[ApiProperties.Static] = YesNo(member, "static");
        properties[ApiProperties.Explicit] = YesNo(member, "explicit");
        properties[ApiProperties.Constexpr] = YesNo(member, "constexpr");
        properties[ApiProperties.Nodiscard] = YesNo(member, "nodiscard");
        properties[ApiProperties.Noexcept] = Signature.NoexceptSpecification(tail) ?? ((string?)member.Attribute("noexcept") == "yes" ? "noexcept" : "no");

        var virtualness = (string?)member.Attribute("virt") ?? "non-virtual";

        if (Signature.HasToken(tail, "override"))
        {
            virtualness += " override";
        }

        properties[ApiProperties.Virtual] = virtualness;
        properties[ApiProperties.Final] = Signature.HasToken(tail, "final") ? "yes" : "no";
        AddIfNotEmpty(properties, ApiProperties.TemplateParameters, templateParameters);

        // Pure virtual is already covered by virt, but deleted and defaulted functions are only visible in the tail
        if (Signature.PureOrDefinition(tail) is { } definition and not "0")
        {
            properties[ApiProperties.Definition] = definition;
        }

        if (parameters.Any(p => p.Element("defval") is not null))
        {
            properties[ApiProperties.DefaultArguments] = string.Join(", ", parameters.Select(p => Signature.Normalize(p.Element("defval")?.Value) is { Length: > 0 } value ? value : "-"));
        }

        var parameterList = string.Join(", ", parameters.Select(ParameterDeclaration));
        var prefix = string.Join("", new[]
                                     {
                                         properties[ApiProperties.Static] == "yes" ? "static " : "",
                                         virtualness.StartsWith("virtual") || virtualness.StartsWith("pure-virtual") ? "virtual " : "",
                                         properties[ApiProperties.Explicit] == "yes" ? "explicit " : "",
                                         properties[ApiProperties.Constexpr] == "yes" ? "constexpr " : "",
                                         kind == ApiKind.Friend ? "friend " : ""
                                     });
        var returnPart = returnType.Length > 0 ? returnType + " " : string.Empty;
        var tailPart = tail.Length > 0 ? " " + tail : string.Empty;
        var key = $"{parentKey}::{(kind == ApiKind.Friend ? "friend " : "")}{templatePrefix}{name}({parameterTypes}){qualifiers}";

        return new ApiEntity(kind,
                             key,
                             parentKey,
                             scope,
                             name,
                             $"{templatePrefix}{prefix}{returnPart}{name}({parameterList}){tailPart}",
                             properties,
                             Location(member));
    }

    private ApiEntity ReadFriend(XElement member, string scope, string parentKey, string name)
    {
        // Friend functions have an argument list, friend classes only a type like "class"
        if (!string.IsNullOrEmpty(member.Element("argsstring")?.Value))
        {
            return ReadFunction(member, ApiKind.Friend, scope, parentKey, name);
        }

        var type = Signature.Normalize(member.Element("type")?.Value);
        var properties = CommonProperties(member);
        properties[ApiProperties.Type] = type;

        return new ApiEntity(ApiKind.Friend,
                             $"{parentKey}::friend {name}",
                             parentKey,
                             scope,
                             name,
                             $"friend {type} {name}",
                             properties,
                             Location(member));
    }

    private ApiEntity ReadVariable(XElement member, string scope, string parentKey, string name)
    {
        var properties = CommonProperties(member);
        var type = Signature.Normalize(member.Element("type")?.Value);
        var array = Signature.Normalize(member.Element("argsstring")?.Value);
        var initializer = Signature.Normalize(member.Element("initializer")?.Value);

        properties[ApiProperties.Type] = type + array;
        properties[ApiProperties.Static] = YesNo(member, "static");
        properties[ApiProperties.Constexpr] = YesNo(member, "constexpr");
        AddIfNotEmpty(properties, ApiProperties.Initializer, initializer);

        var prefix = (properties[ApiProperties.Static] == "yes" ? "static " : "") +
                     (properties[ApiProperties.Constexpr] == "yes" && !type.Contains("constexpr") ? "constexpr " : "");
        var initializerPart = initializer.Length > 0 ? " " + initializer : string.Empty;

        return new ApiEntity(ApiKind.Variable,
                             $"{parentKey}::{name}",
                             parentKey,
                             scope,
                             name,
                             $"{prefix}{type} {name}{array}{initializerPart}",
                             properties,
                             Location(member));
    }

    private ApiEntity ReadTypedef(XElement member, string scope, string parentKey, string name)
    {
        var properties = CommonProperties(member);
        var definition = Signature.Normalize(member.Element("definition")?.Value);
        var templateParameters = TemplateParameters(member);

        properties[ApiProperties.Type] = Signature.Normalize(member.Element("type")?.Value) + Signature.Normalize(member.Element("argsstring")?.Value);
        AddIfNotEmpty(properties, ApiProperties.TemplateParameters, templateParameters);

        var templatePrefix = templateParameters.Length > 0 ? $"template<{templateParameters}> " : string.Empty;

        return new ApiEntity(ApiKind.Typedef,
                             $"{parentKey}::{name}",
                             parentKey,
                             scope,
                             name,
                             templatePrefix + definition,
                             properties,
                             Location(member));
    }

    private void ReadEnum(XElement member, string scope, string parentKey, string name)
    {
        var properties = CommonProperties(member);
        var scoped = YesNo(member, "strong");
        var underlyingType = Signature.Normalize(member.Element("type")?.Value);
        var key = $"{parentKey}::{name}";
        var qualifiedName = member.Element("qualifiedname")?.Value ?? name;

        properties[ApiProperties.Strong] = scoped;
        AddIfNotEmpty(properties, ApiProperties.Type, underlyingType);

        var underlyingPart = underlyingType.Length > 0 ? " : " + underlyingType : string.Empty;

        Add(new ApiEntity(ApiKind.Enum,
                          key,
                          parentKey,
                          scope,
                          name,
                          $"enum {(scoped == "yes" ? "class " : "")}{name}{underlyingPart}",
                          properties,
                          Location(member)));

        foreach (var value in member.Elements("enumvalue"))
        {
            var valueName = value.Element("name")?.Value ?? string.Empty;
            var initializer = Signature.Normalize(value.Element("initializer")?.Value);
            var valueProperties = new SortedDictionary<string, string>
            {
                [ApiProperties.Deprecated] = IsDeprecated(value) ? "yes" : "no"
            };

            AddIfNotEmpty(valueProperties, ApiProperties.Initializer, initializer);

            Add(new ApiEntity(ApiKind.EnumValue,
                              $"{key}::{valueName}",
                              key,
                              qualifiedName,
                              valueName,
                              initializer.Length > 0 ? $"{valueName} {initializer}" : valueName,
                              valueProperties,
                              Location(member)));
        }
    }

    private ApiEntity ReadMacro(XElement member, string name)
    {
        var parameters = member.Elements("param").Select(p => p.Element("defname")?.Value ?? string.Empty).ToList();
        var parameterList = member.Elements("param").Any() ? $"({string.Join(", ", parameters)})" : string.Empty;
        var initializer = Signature.Normalize(member.Element("initializer")?.Value);
        var properties = new SortedDictionary<string, string>
        {
            [ApiProperties.Deprecated] = IsDeprecated(member) ? "yes" : "no"
        };

        AddIfNotEmpty(properties, ApiProperties.Parameters, parameterList);
        AddIfNotEmpty(properties, ApiProperties.Initializer, initializer);

        return new ApiEntity(ApiKind.Macro,
                             $"#define {name}",
                             string.Empty,
                             string.Empty,
                             name,
                             $"#define {name}{parameterList} {initializer}".TrimEnd(),
                             properties,
                             Location(member));
    }

    private void Add(ApiEntity entity)
    {
        // Doxygen can list the same declaration twice, e.g. for specializations, the first one wins
        _entities.TryAdd(entity.Key, entity);
    }

    private bool IsIncluded(string? protection) => protection switch
    {
        null or "public" or "package" => true,
        "protected" => _options.IncludeProtected,
        _ => false
    };

    // Also matches template specializations, e.g. sf::Utf excludes sf::Utf<8>
    private bool IsExcluded(string qualifiedName) =>
        _options.ExcludedNames.Any(excluded => qualifiedName == excluded ||
                                               qualifiedName.StartsWith(excluded + "::", StringComparison.Ordinal) ||
                                               qualifiedName.StartsWith(excluded + "<", StringComparison.Ordinal));

    private static SortedDictionary<string, string> CommonProperties(XElement member) => new()
    {
        [ApiProperties.Protection] = (string?)member.Attribute("prot") ?? "public",
        [ApiProperties.Deprecated] = IsDeprecated(member) ? "yes" : "no"
    };

    private static bool IsDeprecated(XElement element) =>
        element.Elements("briefdescription")
               .Concat(element.Elements("detaileddescription"))
               .Descendants("xrefsect")
               .Any(x => ((string?)x.Attribute("id"))?.StartsWith("deprecated", StringComparison.Ordinal) == true);

    private static string ParameterType(XElement parameter) =>
        Signature.Normalize(parameter.Element("type")?.Value) + Signature.Normalize(parameter.Element("array")?.Value);

    private static string ParameterDeclaration(XElement parameter)
    {
        var declaration = ParameterType(parameter);
        var name = parameter.Element("declname")?.Value;
        var defaultValue = Signature.Normalize(parameter.Element("defval")?.Value);

        if (!string.IsNullOrEmpty(name))
        {
            declaration += " " + name;
        }

        return defaultValue.Length > 0 ? $"{declaration} = {defaultValue}" : declaration;
    }

    private static string TemplateParameters(XElement element)
    {
        var parameters = element.Element("templateparamlist")?.Elements("param").ToList();

        if (parameters is null)
        {
            return string.Empty;
        }

        return string.Join(", ", parameters.Select(p =>
        {
            var declaration = Signature.Normalize(p.Element("type")?.Value);
            var name = p.Element("declname")?.Value;
            var defaultValue = Signature.Normalize(p.Element("defval")?.Value);

            if (!string.IsNullOrEmpty(name) && !declaration.EndsWith(name, StringComparison.Ordinal))
            {
                declaration += " " + name;
            }

            return defaultValue.Length > 0 ? $"{declaration} = {defaultValue}" : declaration;
        }));
    }

    private static string BaseClasses(XElement definition) =>
        string.Join(", ", definition.Elements("basecompoundref")
                                    .Select(b =>
                                    {
                                        var virtualness = (string?)b.Attribute("virt") == "virtual" ? "virtual " : string.Empty;
                                        return $"{virtualness}{(string?)b.Attribute("prot") ?? "public"} {Signature.Normalize(b.Value)}";
                                    }));

    private static string YesNo(XElement element, string attribute) => (string?)element.Attribute(attribute) == "yes" ? "yes" : "no";

    private static void AddIfNotEmpty(IDictionary<string, string> properties, string name, string value)
    {
        if (value.Length > 0)
        {
            properties[name] = value;
        }
    }

    private static (string Scope, string Name) SplitQualifiedName(string qualifiedName)
    {
        // Template arguments can contain "::" as well, so only split outside of angle brackets
        var depth = 0;

        for (var i = qualifiedName.Length - 1; i > 0; --i)
        {
            switch (qualifiedName[i])
            {
                case '>':
                    ++depth;
                    break;
                case '<':
                    --depth;
                    break;
                case ':' when depth == 0 && qualifiedName[i - 1] == ':':
                    return (qualifiedName[..(i - 1)], qualifiedName[(i + 1)..]);
            }
        }

        return (string.Empty, qualifiedName);
    }

    private static string Location(XElement element)
    {
        var location = element.Element("location");

        if (location is null)
        {
            return string.Empty;
        }

        var file = (string?)location.Attribute("file") ?? string.Empty;
        var line = (string?)location.Attribute("line");
        return line is null ? file : $"{file}:{line}";
    }
}
