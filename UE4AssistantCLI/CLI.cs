using ConsoleAppFramework;
using Newtonsoft.Json;
using System;
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
		[Command("add")]
		public async Task AddItem([Option(0)] string verb)
		{
			/*
				string[] args = verb.Parameters.ToArray();

				if (verb.ItemType == "project")
				{
					AddProject(args[0]);
				}
				if (verb.ItemType == "plugin")
				{
					AddPlugin(args[0]);
				}
				else if (verb.ItemType == "module")
				{
					AddModule(args[0]);
				}
				else if (verb.ItemType == "class")
				{
					AddClass(args[0], args[1]);
				}
				else if (verb.ItemType == "bpfl")
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

					AddClass(functionlibraryname, "UBlueprintFunctionLibrary", false, new List<string>
					{
						"Kismet/BlueprintFunctionLibrary.h"
					});
				}
				else if (verb.ItemType == "interface")
				{
					if (!args[0].EndsWith("Interface"))
						args[0] = args[0] + "Interface";
					AddInterface(args[0]);
				}
				else if (verb.ItemType == "dataasset")
				{
					AddDataAsset(args[0], args.Length < 2 ? "UDataAsset" : args[1]);
				}
			*/
		}

		[Command("init", "Initialize working environment, create Libraries.sln.")]
		public async Task InitProject()
		{
			await InitProject(string.Empty);
		}

		[Command("init", "Initialize working environment, create Libraries.sln.")]
		public async Task InitProject([Option(0)] string UE4Version)
		{
			UnrealItemDescription UnrealItem = UnrealItemDescription.DetectUnrealItem(Directory.GetCurrentDirectory(), UnrealItemType.Project);

			if (UnrealItem == null)
			{
				Console.WriteLine("Command should be run inside project folder.");
				return;// -1;
			}

			Program.InitProject(UnrealItem.RootPath, UE4Version);
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
		public async Task GetUERoot()
		{
			await GetUERoot(string.Empty);
		}

		[Command("get_ue_root", "Get UE root of associated UE build.")]
		public async Task GetUERoot([Option(0)] string ProjectName)
		{
			UnrealItemDescription UnrealItem = UnrealItemDescription.DetectUnrealItem(Directory.GetCurrentDirectory(), ProjectName, UnrealItemType.Project);

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
		public async Task CookProject()
		{
			await Program.CookProject(new UnrealCookSettings[] { UnrealCookSettings.CreateDefaultSettings() });
		}

		[Command("cook", "Cook project with cook settings.")]
		public async Task CookProject([Option(0)] string CookSettings)
		{
			await Program.CookProject(
				JsonConvert.DeserializeObject<UnrealCookSettings[]>(File.ReadAllText(CookSettings)
					, new JsonSerializerSettings
					{
						ObjectCreationHandling = ObjectCreationHandling.Replace
					}));
		}

		[Command("convert", "Convert copied clipboard data to json file.")]
		public async Task ConvertObject()
		{
			if (Console.IsInputRedirected)
			{
				//string line = null;
				//while ((line = Console.In.ReadLine()) != null)
				//{
				//	Console.WriteLine(ConvertToJSON(Clipboard.GetText()));
				//}
			}
			else
			{
				//if (Clipboard.ContainsText())
				//{
				//	string json = ConvertToJSON(Clipboard.GetText());
				//	Clipboard.SetText(json);
				//}
				//else
				//{
				//	string line = null;
				//	while ((line = Console.In.ReadLine()) != null)
				//	{
				//		Console.WriteLine(ConvertToJSON(Clipboard.GetText()));
				//	}
				//}
			}
		}

		[Command("merge", "Launch UE4 diff tool to merge conflict file.")]
		public async Task MergeAsset([Option(0)] string AssetPath)
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

		[Command("move", "Move class to another path.")]
		public static void MoveClass([Option(0)] string OriginalFileName, [Option(1)] string DestinationPath)
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
	}
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}
