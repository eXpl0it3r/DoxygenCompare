using DoxygenCompare.Api;
using DoxygenCompare.Comparison;

namespace DoxygenCompare.Tests;

[TestClass]
public class ApiComparerTests
{
    private static readonly string FixturesDirectory = Path.Combine(AppContext.BaseDirectory, "Fixtures");

    private static ComparisonResult Compare(ApiSurfaceOptions? options = null)
    {
        var oldApi = ApiSurface.Load(Path.Combine(FixturesDirectory, "old", "xml"), options);
        var newApi = ApiSurface.Load(Path.Combine(FixturesDirectory, "new", "xml", "index.xml"), options);
        return ApiComparer.Compare(oldApi, newApi);
    }

    private static ApiChange Single(ComparisonResult result, ApiChangeKind change, string scope, string name) =>
        Assert.ContainsSingle(c => c.Change == change && c.Scope == scope && c.Name == name, result.Changes);

    [TestMethod]
    public void DetectsAddedAndRemovedClasses()
    {
        var result = Compare();

        Assert.AreEqual(ApiKind.Class, Single(result, ApiChangeKind.Added, "lib", "Added").Kind);
        Assert.AreEqual(ApiKind.Class, Single(result, ApiChangeKind.Removed, "lib", "Removed").Kind);
    }

    [TestMethod]
    public void OmitsMembersOfAddedAndRemovedClasses()
    {
        var result = Compare();

        Assert.DoesNotContain(c => c.Scope is "lib::Added" or "lib::Removed", result.Changes);
    }

    [TestMethod]
    public void DetectsAddedOverload()
    {
        var change = Single(Compare(), ApiChangeKind.Added, "lib::Widget", "resize");

        Assert.AreEqual("void resize(long size)", change.NewSignature);
    }

    [TestMethod]
    public void DetectsSignatureChangeOfSingleOverload()
    {
        var change = Single(Compare(), ApiChangeKind.SignatureChanged, "lib::Widget", "count");

        Assert.AreEqual("static int count()", change.OldSignature);
        Assert.AreEqual("static int count(bool all)", change.NewSignature);
    }

    [TestMethod]
    public void DetectsReturnTypeChange()
    {
        var change = Single(Compare(), ApiChangeKind.Changed, "lib::Widget", "value");

        Assert.AreEqual(new PropertyChange(ApiProperties.Type, "int", "long"), Assert.ContainsSingle(change.Properties));
    }

    [TestMethod]
    public void DetectsDefaultArgumentChange()
    {
        var change = Single(Compare(), ApiChangeKind.Changed, "lib::Widget", "scale");

        Assert.AreEqual(new PropertyChange(ApiProperties.DefaultArguments, "1.f", "2.f"), Assert.ContainsSingle(change.Properties));
    }

    [TestMethod]
    public void DetectsDeprecation()
    {
        var change = Single(Compare(), ApiChangeKind.Changed, "lib::Widget", "draw");

        Assert.AreEqual(new PropertyChange(ApiProperties.Deprecated, "no", "yes"), Assert.ContainsSingle(change.Properties));
    }

    [TestMethod]
    public void DetectsVirtualChangeOfProtectedMember()
    {
        var change = Single(Compare(), ApiChangeKind.Changed, "lib::Widget", "update");

        Assert.AreEqual(new PropertyChange(ApiProperties.Virtual, "virtual", "non-virtual"), Assert.ContainsSingle(change.Properties));
    }

    [TestMethod]
    public void ExcludesProtectedMembersWhenRequested()
    {
        var result = Compare(new ApiSurfaceOptions { IncludeProtected = false });

        Assert.DoesNotContain(c => c.Name == "update", result.Changes);
    }

    [TestMethod]
    public void IgnoresPrivateMembers()
    {
        Assert.DoesNotContain(c => c.Name == "secret", Compare().Changes);
    }

    [TestMethod]
    public void DetectsEnumValueChanges()
    {
        var result = Compare();

        Assert.AreEqual(ApiKind.EnumValue, Single(result, ApiChangeKind.Added, "lib::Widget::Mode", "D").Kind);
        Assert.AreEqual(ApiKind.EnumValue, Single(result, ApiChangeKind.Removed, "lib::Widget::Mode", "C").Kind);

        var changed = Single(result, ApiChangeKind.Changed, "lib::Widget::Mode", "B");
        Assert.AreEqual(new PropertyChange(ApiProperties.Initializer, "= 2", "= 3"), Assert.ContainsSingle(changed.Properties));
    }

    [TestMethod]
    public void DetectsTypedefChange()
    {
        var change = Single(Compare(), ApiChangeKind.Changed, "lib::Widget", "Id");

        Assert.AreEqual(ApiKind.Typedef, change.Kind);
        Assert.Contains(new PropertyChange(ApiProperties.Type, "int", "long"), change.Properties);
    }

    [TestMethod]
    public void DetectsAddedVariable()
    {
        var change = Single(Compare(), ApiChangeKind.Added, "lib::Widget", "addedField");

        Assert.AreEqual(ApiKind.Variable, change.Kind);
    }

    [TestMethod]
    public void DetectsNoexceptChangeOfFreeFunction()
    {
        var change = Single(Compare(), ApiChangeKind.Changed, "lib", "freeFunction");

        Assert.Contains(new PropertyChange(ApiProperties.Noexcept, "no", "noexcept"), change.Properties);
    }

    [TestMethod]
    public void DetectsMacroValueChange()
    {
        var change = Single(Compare(), ApiChangeKind.Changed, string.Empty, "LIBRARY_VERSION");

        Assert.AreEqual(new PropertyChange(ApiProperties.Initializer, "1", "2"), Assert.ContainsSingle(change.Properties));
    }

    [TestMethod]
    public void ReportsNothingForUnchangedMembers()
    {
        var result = Compare();

        Assert.DoesNotContain(c => c.Name is "Widget" or "field" or "A", result.Changes);
        Assert.DoesNotContain(c => c.Name == "resize" && c.Change != ApiChangeKind.Added, result.Changes);
    }

    [TestMethod]
    public void ExcludesScopesWhenRequested()
    {
        var result = Compare(new ApiSurfaceOptions { ExcludedNames = ["lib::Widget"] });

        Assert.DoesNotContain(c => c.Scope.StartsWith("lib::Widget", StringComparison.Ordinal), result.Changes);
        Assert.Contains(c => c.Name == "Added", result.Changes);
    }
}
