using DoxygenCompare.Api;
using DoxygenCompare.Comparison;

namespace DoxygenCompare.Tests;

/// <summary>
/// The fixtures use INLINE_INHERITED_MEMB, which copies the base class members into every derived class.
/// </summary>
[TestClass]
public class InheritanceTests
{
    private static readonly string FixturesDirectory = Path.Combine(AppContext.BaseDirectory, "Fixtures");

    [TestMethod]
    public void AttributesAddedBaseMemberToBaseClass()
    {
        var oldApi = ApiSurface.Load(Path.Combine(FixturesDirectory, "old", "xml"));
        var newApi = ApiSurface.Load(Path.Combine(FixturesDirectory, "new", "xml"));

        var change = Assert.ContainsSingle(c => c.Name == "setMiterLimit", ApiComparer.Compare(oldApi, newApi).Changes);

        Assert.AreEqual(ApiChangeKind.Added, change.Change);
        Assert.AreEqual("lib::Shape", change.Scope);
    }

    [TestMethod]
    public void KeepsInheritedMembersOutOfDerivedClass()
    {
        var api = ApiSurface.Load(Path.Combine(FixturesDirectory, "old", "xml"));

        Assert.Contains(e => e.Scope == "lib::Shape" && e.Name == "draw", api.Entities.Values);
        Assert.DoesNotContain(e => e.Scope == "lib::Circle" && e.Name == "draw", api.Entities.Values);
        Assert.Contains(e => e.Scope == "lib::Circle" && e.Name == "radius", api.Entities.Values);
    }
}
