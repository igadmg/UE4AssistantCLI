using ClipboardEx.Win32;
using ConsoleAppFramework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using SystemEx;
using UE4Assistant;

namespace UE4AssistantCLI
{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
	class CLI : ConsoleAppBase
	{
		[Command("add_project", "Create new project.")]
		public async Task AddProject([Option(0, "project name")] string name)
		{
			Program.AddProject(name);
		}

		[Command("add_plugin", "Create and add new plugin to current project.")]
		public async Task AddPlugin([Option(0, "plugin name")] string name)
		{
			Program.AddPlugin(name);
		}

		[Command("add_module", "Add new module to current project or plugin.")]
		public async Task AddModule([Option(0, "module name")] string name)
		{
			Program.AddModule(name);
		}

		[Command("add_class", "Add new class to current module.")]
		public async Task AddClass([Option(0, "class name")] string name, [Option(1, "base class name")] string basename = "UObject")
		{
			Program.AddClass(name, basename);
		}

		[Command("add_bpfl", "Add a function library to current module, if one does not exist.")]
		public async Task AddBpfl()
		{
			UnrealItemDescription UnrealItem = UnrealItemDescription.DetectUnrealItem(Directory.GetCurrentDirectory());
			if (UnrealItem == null)
			{
				Console.WriteLine("This command should be run inside module folder.");
				return;// -1;
			}

			string functionlibraryname = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(UnrealItem.ItemFileName)) + "Statics";

			if (File.Exists(Path.Combine(UnrealItem.ModuleClassesPath, functionlibraryname + ".h")))
			{
				Console.WriteLine("This module already contains function library.");
				return;// -1;
			}

			Program.AddClass(functionlibraryname, "UBlueprintFunctionLibrary", false
				, new List<string>
					{
						"Kismet/BlueprintFunctionLibrary.h"
					});
		}

		[Command("add_interface", "Add new interface to current module.")]
		public async Task AddInterface([Option(0, "interface name")] string name)
		{
			if (!name.EndsWith("Interface"))
				name += "Interface";
			Program.AddInterface(name);
		}

		[Command("add_dataasset", "Add a data asset class with given name.")]
		public async Task AddDataAsset([Option(0, "data asset name")] string name, [Option(1, "data asset base class name")] string basename = "UDataAsset")
		{
			Program.AddDataAsset(name, basename);
		}

		[Command("init", "Initialize working environment, create Libraries.sln.")]
		public async Task InitProject([Option(0, "UE4 version")] string UE4Version = null)
		{
			UnrealItemDescription UnrealItem = UnrealItemDescription.DetectUnrealItem(Directory.GetCurrentDirectory(), UnrealItemType.Project);

			if (UnrealItem == null)
			{
				Console.WriteLine("Command should be run inside project folder.");
				return;// -1;
			}

			Program.InitProject(UnrealItem.RootPath, UE4Version ?? "");
		}

		[Command("clean", "Clean project and plugins from build files.")]
		public async Task CleanProject()
		{
			UnrealItemDescription UnrealItem = UnrealItemDescription.DetectUnrealItem(Directory.GetCurrentDirectory(), UnrealItemType.Project);

			if (UnrealItem == null)
			{
				Console.WriteLine("Command should be run inside project folder.");
				return;// -1;
			}

			Console.WriteLine("Cleaning in {0}.", UnrealItem.RootPath);

			Utilities.DeleteDirectory(Path.Combine(UnrealItem.RootPath, "Binaries"));
			Utilities.DeleteDirectory(Path.Combine(UnrealItem.RootPath, "Build"));
			Utilities.DeleteDirectory(Path.Combine(UnrealItem.RootPath, "Intermediate"));
			Utilities.DeleteDirectory(Path.Combine(UnrealItem.RootPath, ".vs"));
			Utilities.DeleteFile(Path.Combine(UnrealItem.RootPath, UnrealItem.Name + ".sln"));

			try
			{
				foreach (var plugin in Program.ListUnrealPlugins(UnrealItem.RootPath))
				{
					Utilities.DeleteDirectory(Path.Combine(plugin.RootPath, "Binaries"));
					Utilities.DeleteDirectory(Path.Combine(plugin.RootPath, "Intermediate"));
				}
			}
			catch { }
		}

		[Command("generate", "Generate Source Code Solution for current project.")]
		public async Task GenerateProject()
		{
			UnrealItemDescription UnrealItem = UnrealItemDescription.DetectUnrealItem(Directory.GetCurrentDirectory(), UnrealItemType.Project);

			if (UnrealItem == null)
			{
				Console.WriteLine("Command should be run inside project folder.");
				return;// -1;
			}

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				string UnrealVersionSelector = UnrealEngineInstance.GetUEVersionSelectorPath();
				Console.Out.WriteLine(UnrealVersionSelector);
				Utilities.ExecuteCommandLine(Utilities.EscapeCommandLineArgs(UnrealVersionSelector, "/projectfiles", UnrealItem.FullPath));
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				UnrealEngineInstance UnrealInstance = new UnrealEngineInstance(UnrealItem);
				Utilities.ExecuteCommandLine(string.Join(" "
					, "\"{0}\"".format(UnrealInstance.GenerateProjectFiles)
					, "-project=\"{0}\"".format(UnrealItem.FullPath)
					, "-game"));
			}
		}

		[Command("editor", "Open UE4 Editor for current project.")]
		public async Task OpenEditor()
		{
			UnrealItemDescription UnrealItem = UnrealItemDescription.DetectUnrealItem(Directory.GetCurrentDirectory(), UnrealItemType.Project);

			if (UnrealItem == null)
			{
				Console.WriteLine("Command should be run inside project folder.");
				return;// -1;
			}

			Utilities.ExecuteOpenFile(UnrealItem.FullPath);
		}

		[Command("code", "Open Source Code Editor for current project.")]
		public async Task OpenCode()
		{
			UnrealItemDescription UnrealItem = UnrealItemDescription.DetectUnrealItem(Directory.GetCurrentDirectory(), UnrealItemType.Project);

			if (UnrealItem == null)
			{
				Console.WriteLine("Command should be run inside project folder.");
				return;// -1;
			}

			Utilities.ExecuteOpenFile(Path.Combine(UnrealItem.RootPath, UnrealItem.Name + ".sln"));
		}

		[Command("get_ue_root", "Get UE root of associated UE build.")]
		public async Task GetUERoot([Option(0, "project name")] string ProjectName = null)
		{
			UnrealItemDescription UnrealItem = UnrealItemDescription.DetectUnrealItem(Directory.GetCurrentDirectory(), ProjectName ?? "", UnrealItemType.Project);

			if (UnrealItem == null)
			{
				Console.WriteLine("Command should be run inside project folder.");
				return;// -1;
			}

			Console.WriteLine(new UnrealEngineInstance(UnrealItem).RootPath);
		}

		[Command("build", "Build project.")]
		public async Task BuildProject()
		{
			using var SleepGuard = Utilities.PreventSleep();

			UnrealItemDescription UnrealItem = UnrealItemDescription.DetectUnrealItem(Directory.GetCurrentDirectory(), UnrealItemType.Project);
			UnrealCookSettings BuildSettings = UnrealCookSettings.CreateBuildSettings();

			UnrealEngineInstance UnrealInstance = new UnrealEngineInstance(UnrealItem);
			BuildSettings.UE4RootPath = Path.GetFullPath(UnrealInstance.RootPath);
			BuildSettings.ProjectFullPath = Path.GetFullPath(UnrealItem.FullPath);

			Utilities.ExecuteCommandLine(string.Format("\"{0}\" BuildCookRun {1}", UnrealInstance.RunUATPath, BuildSettings));
		}

		[Command("cook", "Cook project with cook settings.")]
		public async Task CookProject([Option(0, "cook settings json file name")] string CookSettings = null)
		{
			await Program.CookProject(!CookSettings.null_ws_()
				? JsonConvert.DeserializeObject<UnrealCookSettings[]>(File.ReadAllText(CookSettings)
					, new JsonSerializerSettings
					{
						ObjectCreationHandling = ObjectCreationHandling.Replace
					})
				: new UnrealCookSettings[] { UnrealCookSettings.CreateDefaultSettings() });
		}

		[Command("convert", "Convert copied clipboard data to json file.")]
		public async Task ConvertObject()
		{
			string text = ClipboardEx.GetConsoleOrClipboardText(out var toClipboard);
			string json = Program.ConvertToJSON(text);
			if (toClipboard)
			{
				using var clipboard = new Clipboard();
				clipboard.Text = json;
			}
			else
			{
				Console.WriteLine(json);
			}
		}

		[Command("merge", "Launch UE4 diff tool to merge conflict file.")]
		public async Task MergeAsset([Option(0, "asset path")] string AssetPath)
		{
			UnrealItemDescription UnrealItem = UnrealItemDescription.DetectUnrealItem(Directory.GetCurrentDirectory(), UnrealItemType.Project);
			UnrealEngineInstance UnrealInstance = new UnrealEngineInstance(UnrealItem);

			string savedDiffPath = Path.Combine(UnrealItem.RootPath, "Saved\\Diff");
			if (!Directory.Exists(savedDiffPath))
				Directory.CreateDirectory(savedDiffPath);

			string file = AssetPath;
			string fileName = Path.GetFileName(file).Replace('.', '_');
			string baseFile = Path.Combine(savedDiffPath, string.Format("{0}.Base.uasset", fileName));
			string localFile = Path.Combine(savedDiffPath, string.Format("{0}.Local.uasset", fileName));
			string remoteFile = Path.Combine(savedDiffPath, string.Format("{0}.Remote.uasset", fileName));
			string resultFile = Path.Combine(savedDiffPath, string.Format("{0}.Result.uasset", fileName));

			Utilities.ExecuteCommandLine(string.Format("git show :1:./{0} | git lfs smudge > {1}", file, baseFile));
			Utilities.ExecuteCommandLine(string.Format("git show :2:./{0} | git lfs smudge > {1}", file, localFile));
			Utilities.ExecuteCommandLine(string.Format("git show :3:./{0} | git lfs smudge > {1}", file, remoteFile));
			Utilities.ExecuteCommandLine(string.Format("git show :1:./{0} | git lfs smudge > {1}", file, resultFile));

			Utilities.ExecuteCommandLine(Utilities.EscapeCommandLineArgs(
				UnrealInstance.UE4EditorPath, UnrealItem.FullPath, "-diff", remoteFile, localFile, baseFile, resultFile));

			var baseMd5 = Utilities.CalculateMD5(baseFile);
			var resultMd5 = Utilities.CalculateMD5(resultFile);

			if (!baseMd5.SequenceEqual(resultMd5))
			{
				File.Copy(resultFile, Path.GetFullPath(file), true);
			}

			File.Delete(baseFile);
			File.Delete(localFile);
			File.Delete(remoteFile);
			File.Delete(resultFile);
		}

		[Command("move", "Move class to another path. NOT IMPLEMENTED.")]
		public async Task MoveClass([Option(0)] string OriginalFileName, [Option(1)] string DestinationPath)
		{
			UnrealItemDescription UnrealItem = UnrealItemDescription.DetectUnrealItem(Directory.GetCurrentDirectory(), UnrealItemType.Module);

			if (UnrealItem == null)
			{
				Console.WriteLine("Command should be run inside project module folder.");
				return;// -1;
			}

			string originalFilePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), OriginalFileName));
			string destinationFilePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), DestinationPath));

			if (File.Exists(destinationFilePath))
			{
				Console.WriteLine("Destination path should be a folder.");
				return;// -1;
			}

			if (!Directory.Exists(destinationFilePath))
			{
				//Directory.CreateDirectory()
			}

			UnrealItemPath originalItem = new UnrealItemPath(UnrealItem, originalFilePath);
			UnrealItemPath destiantionItem = new UnrealItemPath(UnrealItem, destinationFilePath);

			// TODO: implement
		}

		[Command("vs_pch_cleanup", "Cleanup PCH file errors.")]
		public async Task VsPCHCleanup()
		{

		}
	}
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}
