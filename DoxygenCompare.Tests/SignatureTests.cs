using DoxygenCompare.Api;

namespace DoxygenCompare.Tests;

[TestClass]
public class SignatureTests
{
    [TestMethod]
    [DataRow("const Texture &", "const Texture&")]
    [DataRow("Color &fillColor", "Color& fillColor")]
    [DataRow("std::vector< std::vector< int > >", "std::vector<std::vector<int>>")]
    [DataRow("void(*)(void *)", "void(*)(void*)")]
    [DataRow("using sf::Id=int", "using sf::Id = int")]
    [DataRow("a==b", "a==b")]
    [DataRow("  unsigned   int  ", "unsigned int")]
    public void Normalize(string input, string expected)
    {
        Assert.AreEqual(expected, Signature.Normalize(input));
    }

    [TestMethod]
    [DataRow("(int a, int b=0) const", "(int a, int b=0)", "const")]
    [DataRow("(std::function< void(int)> f) noexcept", "(std::function< void(int)> f)", "noexcept")]
    [DataRow("(const Texture &)=delete", "(const Texture &)", "= delete")]
    [DataRow("[4]", "", "[4]")]
    public void SplitArguments(string input, string parameters, string tail)
    {
        Assert.AreEqual((parameters, tail), Signature.SplitArguments(input));
    }

    [TestMethod]
    [DataRow("const", "const")]
    [DataRow("const &&", "const &&")]
    [DataRow("const noexcept override", "const")]
    [DataRow("= delete", "")]
    public void OverloadQualifiers(string tail, string expected)
    {
        Assert.AreEqual(expected, Signature.OverloadQualifiers(tail));
    }

    [TestMethod]
    public void PureOrDefinition()
    {
        Assert.AreEqual("delete", Signature.PureOrDefinition("= delete"));
        Assert.AreEqual("0", Signature.PureOrDefinition("const = 0"));
        Assert.IsNull(Signature.PureOrDefinition("const"));
    }
}
