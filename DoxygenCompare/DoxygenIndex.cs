using System.Text;
using System.Xml.Serialization;
using Doxygen;

namespace DoxygenCompare;

public class DoxygenIndex
{
    private DoxygenType _doxygenType = new();

    public DoxygenIndex(string filename)
    {
        Parse(filename);
    }

    public void Compare(DoxygenIndex other)
    {
        var oldList = _doxygenType.compound
                                  .Where(c => c.kind is CompoundKind.@class or CompoundKind.@namespace)
                                  .ToList();
        var newList = other._doxygenType
                           .compound
                           .Where(c => c.kind is CompoundKind.@class or CompoundKind.@namespace)
                           .ToList();

        Console.WriteLine("Added Classes / Namespace:");

        foreach (var added in newList.Select(c => c.name).Except(oldList.Select(c => c.name)))
        {
            Console.WriteLine($"- {added}");
        }

        Console.WriteLine("Removed Classes / Namespace:");

        foreach (var removed in oldList.Select(c => c.name).Except(newList.Select(c => c.name)))
        {
            Console.WriteLine($"- {removed}");
        }

        CompareMembers(oldList, newList);
    }

    private static void CompareMembers(List<CompoundType> oldList, List<CompoundType> newList)
    {
        Console.WriteLine("\nMembers:");

        foreach (var compound in oldList.Where(c => c.kind is CompoundKind.@class or CompoundKind.@namespace)
                                        .Where(c => newList.Exists(n => n.name == c.name)))
        {
            var otherCompound = newList.Single(c => c.name == compound.name);

            var oldFunctions = compound.member is null
                ? []
                : compound.member
                          .Where(m => m.kind == MemberKind.function)
                          .ToList();
            var newFunctions = otherCompound.member is null
                ? []
                : otherCompound
                  .member
                  .Where(m => m.kind == MemberKind.function)
                  .ToList();
            var addedFunctions = newFunctions.Select(m => m.name).Except(oldFunctions.Select(m => m.name))
                                             .ToList();
            var removedFunctions = oldFunctions.Select(m => m.name).Except(newFunctions.Select(m => m.name))
                                               .ToList();

            if (addedFunctions.Count == 0 && removedFunctions.Count == 0)
            {
                continue;
            }

            Console.WriteLine($"Changes for {compound.name}:");

            if (addedFunctions.Count != 0)
            {
                Console.WriteLine("- Added Functions:");

                foreach (var added in addedFunctions)
                {
                    Console.WriteLine($"  - {added}()");
                }
            }

            if (removedFunctions.Count != 0)
            {
                Console.WriteLine("- Removed Functions:");

                foreach (var removed in oldFunctions.Select(m => m.name).Except(newFunctions.Select(m => m.name)))
                {
                    Console.WriteLine($"  - {removed}()");
                }
            }
        }
    }

    public override string ToString()
    {
        var builder = new StringBuilder();

        foreach (var compoundKindValue in Enum.GetValues(typeof(CompoundKind)))
        {
            builder.Append($"{(CompoundKind)compoundKindValue}\n");

            foreach (var compoundType in _doxygenType.compound.Where(c => c.kind == (CompoundKind)compoundKindValue))
            {
                builder.Append($"\t{compoundType.name}\n");

                if (compoundType.member == null)
                {
                    continue;
                }

                foreach (var memberKindValue in Enum.GetValues(typeof(MemberKind)))
                {
                    builder.Append($"\t\t{(MemberKind)memberKindValue}\n");

                    foreach (var memberType in compoundType.member.Where(m => m.kind == (MemberKind)memberKindValue))
                    {
                        builder.Append($"\t\t\t{memberType.name}\n");
                    }
                }
            }
        }

        return builder.ToString();
    }

    private void Parse(string filename)
    {
        var fileData = File.ReadAllBytes(filename);
        using var memoryStream = new MemoryStream(fileData);
        var serializer = new XmlSerializer(typeof(DoxygenType));
        _doxygenType = (DoxygenType)serializer.Deserialize(memoryStream);
    }
}