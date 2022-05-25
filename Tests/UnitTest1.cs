using SystemEx;
using UE4Assistant;
using UE4AssistantCLI.UI;

namespace Tests;

public class Tests
{
    [SetUp]
    public void Setup()
    {
        var str = "UFUNCTION(Category = \"sdfsd\", BlueprintPure, meta=(DefaultToSelf = \"Object\"))";

        if (Specifier.TryParse(str.tokenize(), out var specifier))
        {
            var so = new SpeсifierTypeDescriptor(specifier);
        }
    }

    [Test]
    public void Test1()
    {
        Assert.Pass();
    }
}