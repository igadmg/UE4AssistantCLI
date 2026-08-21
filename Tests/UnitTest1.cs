using Newtonsoft.Json.Linq;
using UE4Assistant;
using UE4AssistantCLI;

namespace Tests;

public class UtilitiesTests
{
	[Test]
	public void ParseDllLoadErrors_ReturnsFailedLoadPaths()
	{
		const string log = "LogWindows: Failed to load 'C:/Project/Binaries/Win64/Example.dll' (GetLastError=126)\n" +
			"LogWindows: Failed to preload 'C:/Project/Binaries/Win64/Plugin.dll'";

		var files = log.ParseDllLoadErrors().ToArray();

		Assert.That(files, Is.EqualTo(new[]
		{
			"C:/Project/Binaries/Win64/Example.dll",
			"C:/Project/Binaries/Win64/Plugin.dll"
		}));
	}

	[Test]
	public void ParsePCHFilesErrors_ReturnsPchPaths()
	{
		const string log = "fatal error C1853: 'C:/Project/Intermediate/Build/Win64/Example.pch' precompiled header file is from a previous version";

		var files = log.ParsePCHFilesErrors().ToArray();

		Assert.That(files, Is.EqualTo(new[] { "C:/Project/Intermediate/Build/Win64/Example.pch" }));
	}
}

public class UnrealItemDescriptionTests
{
	[Test]
	public void DetectUnrealItem_FindsProjectInCurrentDirectory()
	{
		using var directory = new TempDirectory();
		var projectFile = Path.Combine(directory.Path, "Sample.uproject");
		File.WriteAllText(projectFile, "{}");

		var item = UnrealItemDescription.DetectUnrealItem(directory.Path, UnrealItemType.Project);

		Assert.That(item, Is.Not.Null);
		Assert.Multiple(() =>
		{
			Assert.That(item!.Type, Is.EqualTo(UnrealItemType.Project));
			Assert.That(item.Name, Is.EqualTo("Sample"));
			Assert.That(item.FullPath, Is.EqualTo(projectFile));
		});
	}

	[Test]
	public void DetectUnrealItem_FindsModuleInParentDirectory()
	{
		using var directory = new TempDirectory();
		var moduleFile = Path.Combine(directory.Path, "Sample.Build.cs");
		var childDirectory = Path.Combine(directory.Path, "Private");
		File.WriteAllText(moduleFile, string.Empty);
		Directory.CreateDirectory(childDirectory);

		var item = UnrealItemDescription.DetectUnrealItem(childDirectory, UnrealItemType.Module);

		Assert.That(item, Is.Not.Null);
		Assert.Multiple(() =>
		{
			Assert.That(item!.Type, Is.EqualTo(UnrealItemType.Module));
			Assert.That(item.Name, Is.EqualTo("Sample"));
			Assert.That(item.FullPath, Is.EqualTo(moduleFile));
		});
	}
}

public class ProgramConversionTests
{
	[Test]
	public void ConvertToJSON_ConvertsSimpleObject()
	{
		var json = Program.ConvertToJSON("(Name=Example,Enabled=True)");
		var result = JObject.Parse(json);

		Assert.Multiple(() =>
		{
			Assert.That((string?)result["Name"], Is.EqualTo("Example"));
			Assert.That((string?)result["Enabled"], Is.EqualTo("True"));
		});
	}

	[Test]
	public void ConvertToJSON_ConvertsNestedObject()
	{
		var json = Program.ConvertToJSON("(Outer=(Inner=Value),Text=Hello)");
		var result = JObject.Parse(json);

		Assert.Multiple(() =>
		{
			Assert.That((string?)result["Outer"]?["Inner"], Is.EqualTo("Value"));
			Assert.That((string?)result["Text"], Is.EqualTo("Hello"));
		});
	}
}

public sealed class TempDirectory : IDisposable
{
	public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"UE4AssistantCLI.Tests.{Guid.NewGuid():N}");

	public TempDirectory()
	{
		Directory.CreateDirectory(Path);
	}

	public void Dispose()
	{
		if (Directory.Exists(Path))
		{
			Directory.Delete(Path, true);
		}
	}
}
