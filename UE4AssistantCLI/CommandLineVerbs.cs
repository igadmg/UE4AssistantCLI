using CommandLine;
using CommandLine.Text;
using System.Collections.Generic;



namespace UE4AssistantCLI
{
	[Verb("add", HelpText = "Add item to the project.")]
	internal class AddVerb
	{
		[Value(0, Required = true, HelpText = "Item type.")]
		public string ItemType { get; set; }

		[Value(1, Required = false, HelpText = "Item parameters.", Hidden = true)]
		public IEnumerable<string> Parameters { get; set; }

		[Usage]
		public static IEnumerable<Example> Examples {
			get {
				yield return new Example("Create new project", new AddVerb { ItemType = "project", Parameters = new string[] { "<ProjectName>" } });
				yield return new Example("Create and add new plugin to current project", new AddVerb { ItemType = "plugin", Parameters = new string[] { "<PluginName>" } });
				yield return new Example("Add new module to current project or plugin", new AddVerb { ItemType = "module", Parameters = new string[] { "<ModuleName>" } });
				yield return new Example("Add new interface to current module", new AddVerb { ItemType = "interface", Parameters = new string[] { "<InterfaceName>" } });
				yield return new Example("Add new class to current module", new AddVerb { ItemType = "class", Parameters = new string[] { "<ClassName> <BaseType>" } });
				yield return new Example("Add a function library to current module, if one does not exist", new AddVerb { ItemType = "bpfl" });
				yield return new Example("Add a data asset class with given name. BaseType is optional", new AddVerb { ItemType = "dataasset", Parameters = new string[] { "<ClassName> <BaseType>" } });
			}
		}
	}

	[Verb("init", HelpText = "Initialize working environment, create Libraries.sln.")]
	internal class InitVerb
	{
		[Value(0, Required = false, HelpText = "UE4 version or GUID.")]
		public string UE4Version { get; set; }
	}

	[Verb("clean", HelpText = "Clean project and plugins from build files.")]
	internal class CleanVerb
	{
	}

	[Verb("generate", HelpText = "Generate Source Code Solution for current project.")]
	internal class GenerateVerb
	{
	}

	[Verb("editor", HelpText = "Open UE4 Editor for current project.")]
	internal class EditorVerb
	{
	}

	[Verb("code", HelpText = "Open Source Code Editor for current project.")]
	internal class CodeVerb
	{
	}

	[Verb("get_ue_root", HelpText = "Get UE root of associated UE build.")]
	internal class GetUERootVerb
	{
		[Value(0, Required = false)]
		public string ProjectName { get; set; }
	}

	[Verb("build", HelpText = "Build project.")]
	internal class BuildVerb
	{
	}

	[Verb("cook", HelpText = "Cook project with cook settings.")]
	internal class CookVerb
	{
		[Value(0, Required = false)]
		public string CookSettings { get; set; }
	}

	[Verb("convert", HelpText = "Convert copied clipboard data to json file.")]
	internal class ConvertVerb
	{
	}

	[Verb("merge", HelpText = "Launch UE4 diff tool to merge conflict file.")]
	internal class MergeVerb
	{
		[Value(0, Required = true)]
		public string AssetPath { get; set; }
	}

	[Verb("move", HelpText = "Move class to another path.")]
	internal class MoveVerb
	{
		[Value(0, Required = true, HelpText = "Original class path with name.")]
		public string OriginalFileName { get; set; }

		[Value(1, Required = true, HelpText = "Destination path.")]
		public string DestinationPath { get; set; }
	}
}
