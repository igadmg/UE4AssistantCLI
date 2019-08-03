using CommandLine;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using SystemEx;
using UE4Assistant;
using UE4Assistant.Templates.Generators;
using Template = UE4Assistant.Template;

namespace UE4AssistantCLI
{
	class UnrealCookSettings
	{
		public string UE4RootPath = null;
		public string ProjectFullPath = null;
		public string Platform = null;
		public string CookFlavor = null;
		public string ClientConfig;
		public string ServerConfig;
		public bool? UseP4 = null;
		public bool? Cook = null;
		public bool? AllMaps = null;
		public bool? Client = null;
		public bool? Server = null;
		public bool? Build = null;
		public bool? Stage = null;
		public bool? Pak = null;
		public bool? Archive = null;
		public bool? Package = null;
		public bool? Compressed = null;
		public string ArchiveDirectory;

		public static UnrealCookSettings CreateDefaultSettings()
		{
			return new UnrealCookSettings
			{
				UE4RootPath = null,
				ProjectFullPath = null,
				UseP4 = false,
				Platform = "Win64",
				ClientConfig = "Development",
				ServerConfig = "Development",
				Cook = true,
				AllMaps = true,
				Server = false,
				Build = true,
				Stage = true,
				Pak = true,
				Archive = true,
				Package = true,
				ArchiveDirectory = "./Packages",
			};
		}

		public static UnrealCookSettings CreateBuildSettings()
		{
			return new UnrealCookSettings
			{
				UE4RootPath = null,
				ProjectFullPath = null,
				UseP4 = false,
				Platform = "Win64",
				ClientConfig = "Development",
				ServerConfig = "Development",
				Cook = false,
				AllMaps = true,
				Server = false,
				Build = true,
				Stage = false,
				Pak = false,
				Archive = false,
				Package = false,
				ArchiveDirectory = null,
			};
		}

		public override string ToString()
		{
			return (!string.IsNullOrWhiteSpace(ProjectFullPath) ? string.Format("-project=\"{0}\"", ProjectFullPath) : "")
				+ (Platform != null ? string.Format(" -platform=\"{0}\"", Platform) : "")
				+ (CookFlavor != null ? string.Format(" -cookflavor=\"{0}\"", CookFlavor) : "")
				+ string.Format(" -clientconfig=\"{0}\"", ClientConfig)
				+ string.Format(" -serverconfig=\"{0}\"", ServerConfig)
				+ (UseP4 != null ? UseP4.Value ? " -P4" : " -noP4" : "")
				+ (Cook != null ? Cook.Value ? " -cook" : "" : "")
				+ (AllMaps != null ? AllMaps.Value ? " -allmaps" : "" : "")
				+ (Client != null ? Client.Value ? " -client" : " -noclient" : "")
				+ (Server != null ? Server.Value ? " -server" : " -noserver" : "")
				+ (Build != null ? Build.Value ? " -build" : "" : "")
				+ (Stage != null ? Stage.Value ? " -stage" : "" : "")
				+ (Pak != null ? Pak.Value ? " -pak" : "" : "")
				+ (Archive != null ? Archive.Value ? " -archive" : "" : "")
				+ (Package != null ? Package.Value ? " -package" : "" : "")
				+ (Compressed != null ? Compressed.Value ? " -compressed" : "" : "")
				+ (!string.IsNullOrWhiteSpace(ArchiveDirectory) ? string.Format(" -archivedirectory=\"{0}\"", ArchiveDirectory) : "")
				;
		}
	}

	class Program
	{
		static void PrintUsage()
		{
			Console.Error.WriteLine(string.Format(System.Globalization.CultureInfo.InvariantCulture
				, "ua-cli current version: {0:n0}"
				, System.Reflection.Assembly.GetExecutingAssembly().GetLinkerTime().Ticks / 1000000).Replace(',', '.'));
			Console.Error.WriteLine("ua-cli <command> <parameters>");
			Console.Error.WriteLine("    add <item> <params> - add something to the project");
			Console.Error.WriteLine("available items to add:");
			Console.Error.WriteLine("        project <ProjectName> - create new project");
			Console.Error.WriteLine("        plugin <PluginName> - create and add new plugin to current project.");
			Console.Error.WriteLine("        module <ModuleName> - add new module to current project or plugin.");
			Console.Error.WriteLine("        interface <InterfaceName> - add new interface to current module.");
			Console.Error.WriteLine("        class <ClassName> <BaseType> - add new class to current module.");
			Console.Error.WriteLine("        bpfl - add a function library to current module, if one does not exist.");
			Console.Error.WriteLine("        dataasset <ClassName> <BaseType> - add a data asset class with given name. BaseType is optional.");
			Console.Error.WriteLine("");
			Console.Error.WriteLine("    init <vs version> - initialize working environment, create Libraries.sln.");
			Console.Error.WriteLine("    clean - clean project and plugins from build files.");
			Console.Error.WriteLine("    generate - generate VS project for project.");
			Console.Error.WriteLine("    get_ue_root <ProjectName> - get UE root of associated UE build.");
			Console.Error.WriteLine("    build - build project.");
			Console.Error.WriteLine("    cook <CookSettings> - cook project with cook settings.");
			Console.Error.WriteLine("");
			Console.Error.WriteLine("    convert - copied clipboard data to json file.");
			Console.Error.WriteLine("");
			Console.Error.WriteLine("    merge - launch UE4 diff tool to merge conflict file.");
		}

		[STAThread]
		static int Main(string[] args)
		{
			/*
			if (args.Length == 0)
			{
				PrintUsage();
				return -1;
			}
			*/

			Parser.Default.ParseArguments(args
				, typeof(AddVerb)
				, typeof(InitVerb)
				, typeof(CleanVerb)
				, typeof(GenerateVerb)
				, typeof(EditorVerb)
				, typeof(CodeVerb)
				, typeof(GetUERootVerb)
				, typeof(BuildVerb)
				, typeof(CookVerb)
				, typeof(ConvertVerb)
				, typeof(MergeVerb)
				, typeof(MoveVerb)
				)
				.WithParsed<AddVerb>(v => AddItem(v))
				.WithParsed<InitVerb>(v => InitProject(v))
				.WithParsed<CleanVerb>(v => CleanProject(v))
				.WithParsed<GenerateVerb>(v => GenerateProject(v))
				.WithParsed<EditorVerb>(v => OpenEditor(v))
				.WithParsed<CodeVerb>(v => OpenCode(v))
				.WithParsed<GetUERootVerb>(v => GetUERoot(v))
				.WithParsed<BuildVerb>(v => BuildProject(v))
				.WithParsed<CookVerb>(v => CookProject(v))
				.WithParsed<ConvertVerb>(v => ConvertObject(v))
				.WithParsed<MergeVerb>(v => MergeAsset(v))
				.WithParsed<MoveVerb>(v => MoveClass(v));

			return 0;
		}

		static IEnumerable<UnrealItemDescription> ListUnrealPlugins(string path)
		{
			foreach (string file in Directory.GetFiles(path, "*.uplugin", SearchOption.AllDirectories))
			{
				yield return new UnrealItemDescription(UnrealItemType.Plugin, file);
			}

			yield break;
		}

		private static void AddItem(AddVerb verb)
		{
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
		}

		private static void AddProject(string projectname)
		{
			Utilities.ExecuteCommandLine("git init");

			UProject project = Template.CreateProject(projectname);
		}

		private static void AddPlugin(string pluginname)
		{
			UnrealItemDescription UnrealItem = UnrealItemDescription.DetectUnrealItem(Directory.GetCurrentDirectory(), UnrealItemType.Project);
			if (UnrealItem == null)
			{
				Console.WriteLine("This command should be run inside project or plugin folder.");
				return;
			}

			string pluginDirectory = Path.Combine(UnrealItem.RootPath, "Plugins", pluginname);
			if (Directory.Exists(pluginDirectory))
			{
				Console.WriteLine("\"{0}\" should not exist.".format(pluginDirectory));
				return;
			}

			Directory.CreateDirectory(pluginDirectory);
			using (Utilities.SetCurrentDirectory(pluginDirectory))
			{
				UPlugin plugin = new UPlugin();
				plugin.Save(Path.Combine(pluginDirectory, pluginname + ".uplugin"));

				AddModule(pluginname);
			}
		}

		private static void AddModule(string modulename)
		{
			UnrealItemDescription UnrealItem = UnrealItemDescription.DetectUnrealItem(Directory.GetCurrentDirectory(), UnrealItemType.Project, UnrealItemType.Plugin);
			if (UnrealItem == null)
			{
				Console.WriteLine("This command should be run inside project or plugin folder.");
				return;
			}
			else
			{
				string itemFullPath = UnrealItem.FullPath;
				if (UnrealItem.Type == UnrealItemType.Plugin)
				{
					UPlugin plugin = UPlugin.Load(itemFullPath);

					UModule module = new UModule();
					module.Name = modulename;
					Template.CreateModule(plugin.RootPath, module.Name);
					plugin.Modules.Add(module);

					plugin.Save(itemFullPath);
				}
				else if (UnrealItem.Type == UnrealItemType.Project)
				{
					UProject project = UProject.Load(itemFullPath);

					UModule module = new UModule();
					module.Name = modulename;
					Template.CreateModule(project.RootPath, module.Name);
					project.Modules.Add(module);

					project.Save(itemFullPath);
				}
			}
		}

		private static void AddClass(string objectname, string basename)
		{
			AddClass(objectname, basename, true, new List<string>());
		}

		private static void AddClass(string objectname, string basename, bool hasConstructor, List<string> extraincludes)
		{
			string typename = basename[0].ToString();
			string objectfolder = Directory.GetCurrentDirectory();

			UnrealItemDescription UnrealItem = UnrealItemDescription.DetectUnrealItem(objectfolder, UnrealItemType.Module);
			if (UnrealItem == null)
			{
				Console.WriteLine("This command should be run inside module folder.");
				return;
			}

			string modulename = UnrealItem.Name;
			string objectpath = new UnrealItemPath(UnrealItem, objectfolder).ItemPath;

			Dictionary<string, object> parameters = new Dictionary<string, object>
				{
					{ "modulename", modulename },
					{ "objectname", objectname },
					{ "objectpath", objectpath.Replace('\\', '/') + (objectpath.null_() ? "" : "/") },
					{ "extraincludes", extraincludes },
					{ "basename", basename },
					{ "typename", typename },
					{ "hasConstructor", hasConstructor }
				};

			string classesPath = Path.Combine(UnrealItem.ModuleClassesPath, objectpath);
			string privatePath = Path.Combine(UnrealItem.ModulePrivatePath, objectpath);

			Directory.CreateDirectory(classesPath);
			Directory.CreateDirectory(privatePath);

			if (typename == "U" || typename == "A")
			{
				File.WriteAllText(Path.Combine(classesPath, objectname + ".h")
					, Template.TransformToText<Class_h>(parameters));
				File.WriteAllText(Path.Combine(privatePath, objectname + ".cpp")
					, Template.TransformToText<Class_cpp>(parameters));
			}
			else
			{
				File.WriteAllText(Path.Combine(classesPath, objectname + ".h")
					, Template.TransformToText<SimpleClass_h>(parameters));
				File.WriteAllText(Path.Combine(privatePath, objectname + ".cpp")
					, Template.TransformToText<SimpleClass_cpp>(parameters));
			}
		}

		private static void AddInterface(string objectname)
		{
			string objectfolder = Directory.GetCurrentDirectory();

			UnrealItemDescription UnrealItem = UnrealItemDescription.DetectUnrealItem(objectfolder, UnrealItemType.Module);
			if (UnrealItem == null)
			{
				Console.WriteLine("This command should be run inside module folder.");
				return;
			}

			string modulename = UnrealItem.Name;
			string objectpath = new UnrealItemPath(UnrealItem, objectfolder).ItemPath;

			Dictionary<string, object> parameters = new Dictionary<string, object>
				{
					{ "modulename", modulename },
					{ "objectname", objectname },
					{ "objectpath", objectpath.Replace('\\', '/') + (objectpath.null_() ? "" : "/") },
				};

			string classesPath = Path.Combine(UnrealItem.ModuleClassesPath, objectpath);

			Directory.CreateDirectory(classesPath);

			File.WriteAllText(Path.Combine(classesPath, objectname + ".h")
				, Template.TransformToText<Interface_h>(parameters));
		}

		private static void AddDataAsset(string objectname, string basename)
		{
			string objectfolder = Directory.GetCurrentDirectory();

			UnrealItemDescription UnrealItem = UnrealItemDescription.DetectUnrealItem(objectfolder, UnrealItemType.Module);
			if (UnrealItem == null)
			{
				Console.WriteLine("This command should be run inside module folder.");
				return;
			}

			string modulename = UnrealItem.Name;
			string objectpath = new UnrealItemPath(UnrealItem, objectfolder).ItemPath;

			Dictionary<string, object> parameters = new Dictionary<string, object>
				{
					{ "modulename", modulename },
					{ "objectname", objectname },
					{ "objectpath", objectpath.Replace('\\', '/') + (objectpath.null_() ? "" : "/") },
					{ "basename", basename },
					{ "typename", "U" },
				};

			string classesPath = Path.Combine(UnrealItem.ModuleClassesPath, objectpath);

			Directory.CreateDirectory(classesPath);

			File.WriteAllText(Path.Combine(classesPath, objectname + ".h")
				, Template.TransformToText<Class_h>(parameters));
		}

		class VSProject
		{
			public string path;
			public string guid;
			public List<string> configurations = new List<string>();
		}

		private static void InitProject(string path, string suffix)
		{
			List<VSProject> vsprojects = new List<VSProject>();
			List<string> vsconfigurations = new List<string>();

			string vcxprojSuffix = string.Format("VisualStudio{0}.vcxproj", string.IsNullOrWhiteSpace(suffix) ? "" : (".vs" + suffix));

			var projects = Directory.GetFiles(path, "*.vcxproj", SearchOption.AllDirectories)
				.Where(p => p.Contains("Build-") && p.EndsWith(vcxprojSuffix));

			foreach (var project in projects)
			{
				XmlDocument xml = new XmlDocument();
				xml.Load(project);
				XmlNamespaceManager nsmgr = new XmlNamespaceManager(xml.NameTable);
				nsmgr.AddNamespace("msbuild", "http://schemas.microsoft.com/developer/msbuild/2003");


				VSProject vsproject = new VSProject();
				vsproject.path = project.Remove(0, path.Length + 1);
				vsproject.guid = xml.SelectSingleNode("//msbuild:Project/*/msbuild:ProjectGuid", nsmgr).InnerText;
				foreach (XmlAttribute conf in xml.SelectNodes("//msbuild:Project/*/msbuild:ProjectConfiguration/@Include", nsmgr))
				{
					if (!vsconfigurations.Contains(conf.Value))
						vsconfigurations.Add(conf.Value);

					vsproject.configurations.Add(conf.Value);
				}

				vsprojects.Add(vsproject);
			}

			using (StreamWriter writetext = new StreamWriter(Path.Combine(path, string.Format("Libraries{0}.sln", string.IsNullOrWhiteSpace(suffix) ? "" : (".vs" + suffix))), false, Encoding.UTF8))
			{
				writetext.WriteLine("");
				writetext.WriteLine("Microsoft Visual Studio Solution File, Format Version 12.00");
				writetext.WriteLine("# Visual Studio {0}", string.IsNullOrWhiteSpace(suffix) ? "14" : suffix);

				writetext.WriteLine("Project(\"{2150E333-8FDC-42A3-9474-1A3956D46DE8}\") = \"Libraries\", \"Libraries\", \"{C7BD0C56-AECE-4CAE-9A64-473DE0824E80}\"");
				writetext.WriteLine("EndProject");

				foreach (var vsproject in vsprojects)
				{
					writetext.WriteLine("Project(\"{{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}}\") = \"{0}\", \"{1}\", \"{2}\"", Path.GetFileNameWithoutExtension(vsproject.path), vsproject.path, vsproject.guid);
					writetext.WriteLine("EndProject");
				}

				writetext.WriteLine("Global");
				writetext.WriteLine("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");
				foreach (var conf in vsconfigurations)
				{
					writetext.WriteLine("\t\t{0} = {0}", conf);
				}
				writetext.WriteLine("\tEndGlobalSection");

				writetext.WriteLine("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution");
				foreach (var vsproject in vsprojects)
				{
					foreach (var conf in vsproject.configurations)
					{
						writetext.WriteLine("\t\t{0}.{1}.ActiveCfg = {1}", vsproject.guid, conf);
						writetext.WriteLine("\t\t{0}.{1}.Build.0 = {1}", vsproject.guid, conf);
					}
				}
				writetext.WriteLine("\tEndGlobalSection");
				writetext.WriteLine("\tGlobalSection(SolutionProperties) = preSolution");
				writetext.WriteLine("\t\tHideSolutionNode = FALSE");
				writetext.WriteLine("\tEndGlobalSection");
				writetext.WriteLine("\tGlobalSection(NestedProjects) = preSolution");
				foreach (var vsproject in vsprojects)
				{
					writetext.WriteLine("\t\t{0} = {1}", vsproject.guid, "{C7BD0C56-AECE-4CAE-9A64-473DE0824E80}");
				}
				writetext.WriteLine("\tEndGlobalSection");
				writetext.WriteLine("EndGlobal");
			}
		}

		private static void InitProject(InitVerb verb)
		{
			UnrealItemDescription UnrealItem = UnrealItemDescription.DetectUnrealItem(Directory.GetCurrentDirectory(), UnrealItemType.Project);

			if (UnrealItem == null)
			{
				Console.WriteLine("Command should be run inside project folder.");
				return;// -1;
			}

			InitProject(UnrealItem.RootPath, verb.UE4Version);
		}

		private static void CleanProject(CleanVerb verb)
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
				foreach (var plugin in ListUnrealPlugins(UnrealItem.RootPath))
				{
					Utilities.DeleteDirectory(Path.Combine(plugin.RootPath, "Binaries"));
					Utilities.DeleteDirectory(Path.Combine(plugin.RootPath, "Intermediate"));
				}
			}
			catch { }
		}

		private static void GenerateProject(GenerateVerb verb)
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

		private static void OpenEditor(EditorVerb verb)
		{
			UnrealItemDescription UnrealItem = UnrealItemDescription.DetectUnrealItem(Directory.GetCurrentDirectory(), UnrealItemType.Project);

			if (UnrealItem == null)
			{
				Console.WriteLine("Command should be run inside project folder.");
				return;// -1;
			}

			Utilities.ExecuteOpenFile(UnrealItem.FullPath);
		}

		private static void OpenCode(CodeVerb verb)
		{
			UnrealItemDescription UnrealItem = UnrealItemDescription.DetectUnrealItem(Directory.GetCurrentDirectory(), UnrealItemType.Project);

			if (UnrealItem == null)
			{
				Console.WriteLine("Command should be run inside project folder.");
				return;// -1;
			}

			Utilities.ExecuteOpenFile(Path.Combine(UnrealItem.RootPath, UnrealItem.Name + ".sln"));
		}

		private static void GetUERoot(GetUERootVerb verb)
		{
			UnrealItemDescription UnrealItem = UnrealItemDescription.DetectUnrealItem(Directory.GetCurrentDirectory(), verb.ProjectName, UnrealItemType.Project);

			if (UnrealItem == null)
			{
				Console.WriteLine("Command should be run inside project folder.");
				return;// -1;
			}

			Console.WriteLine(new UnrealEngineInstance(UnrealItem).RootPath);
		}

		private static void BuildProject(BuildVerb verb)
		{
			UnrealItemDescription UnrealItem = UnrealItemDescription.DetectUnrealItem(Directory.GetCurrentDirectory(), UnrealItemType.Project);
			UnrealCookSettings BuildSettings = UnrealCookSettings.CreateBuildSettings();

			using (var SleepGuard = Utilities.PreventSleep())
			{
				UnrealEngineInstance UnrealInstance = new UnrealEngineInstance(UnrealItem);
				BuildSettings.UE4RootPath = Path.GetFullPath(UnrealInstance.RootPath);
				BuildSettings.ProjectFullPath = Path.GetFullPath(UnrealItem.FullPath);

				Utilities.ExecuteCommandLine(string.Format("\"{0}\" BuildCookRun {1}", UnrealInstance.RunUATPath, BuildSettings));
			}
		}

		private static void CookProject(CookVerb verb)
		{
			UnrealItemDescription UnrealItem = UnrealItemDescription.DetectUnrealItem(Directory.GetCurrentDirectory(), UnrealItemType.Project);
			UnrealCookSettings[] CookSettings = !verb.CookSettings.null_()
				? JsonConvert.DeserializeObject<UnrealCookSettings[]>(File.ReadAllText(verb.CookSettings)
					, new JsonSerializerSettings
					{
						ObjectCreationHandling = ObjectCreationHandling.Replace
					})
				: new UnrealCookSettings[] { UnrealCookSettings.CreateDefaultSettings() };

			using (var SleepGuard = Utilities.PreventSleep())
			{
				foreach (var Recipe in CookSettings)
				{
					UnrealEngineInstance UnrealInstance = null;
					try { UnrealInstance = new UnrealEngineInstance(Recipe.UE4RootPath); }
					catch { UnrealInstance = new UnrealEngineInstance(UnrealItem); }
					if (string.IsNullOrWhiteSpace(Recipe.ProjectFullPath))
					{
						Recipe.ProjectFullPath = UnrealItem.FullPath;
					}
					Recipe.UE4RootPath = UnrealInstance.RootPath;
					Recipe.ProjectFullPath = Path.GetFullPath(Recipe.ProjectFullPath);
					Recipe.ArchiveDirectory = Path.GetFullPath(Recipe.ArchiveDirectory);

					Utilities.ExecuteCommandLine(string.Format("\"{0}\" BuildCookRun {1}", UnrealInstance.RunUATPath, Recipe));
				}
			}
		}

		private static void ConvertObject(ConvertVerb verb)
		{
			if (Console.IsInputRedirected)
			{
				string line = null;
				while ((line = Console.In.ReadLine()) != null)
				{
					Console.WriteLine(ConvertToJSON(Clipboard.GetText()));
				}
			}
			else
			{
				if (Clipboard.ContainsText())
				{
					string json = ConvertToJSON(Clipboard.GetText());
					Clipboard.SetText(json);
				}
				else
				{
					string line = null;
					while ((line = Console.In.ReadLine()) != null)
					{
						Console.WriteLine(ConvertToJSON(Clipboard.GetText()));
					}
				}
			}
		}

		private static void MergeAsset(MergeVerb verb)
		{
			UnrealItemDescription UnrealItem = UnrealItemDescription.DetectUnrealItem(Directory.GetCurrentDirectory(), UnrealItemType.Project);
			UnrealEngineInstance UnrealInstance = new UnrealEngineInstance(UnrealItem);

			string savedDiffPath = Path.Combine(UnrealItem.RootPath, "Saved\\Diff");
			if (!Directory.Exists(savedDiffPath))
				Directory.CreateDirectory(savedDiffPath);

			string file = verb.AssetPath;
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

		private static void MoveClass(MoveVerb verb)
		{
			UnrealItemDescription UnrealItem = UnrealItemDescription.DetectUnrealItem(Directory.GetCurrentDirectory(), UnrealItemType.Module);

			if (UnrealItem == null)
			{
				Console.WriteLine("Command should be run inside project module folder.");
				return;// -1;
			}

			string originalFilePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), verb.OriginalFileName));
			string destinationFilePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), verb.DestinationPath));

			if (File.Exists(destinationFilePath))
			{
				Console.WriteLine("Destination path should be a folder.");
				return;// -1;
			}

			if (!Directory.Exists(destinationFilePath))
			{
				Directory.CreateDirectory()
			}

			UnrealItemPath originalItem = new UnrealItemPath(UnrealItem, originalFilePath);
			UnrealItemPath destiantionItem = new UnrealItemPath(UnrealItem, destinationFilePath);

			int i = 0;
		}

		private static Dictionary<string, object> ParseUE4Object(string line, ref int li)
		{
			if (line[li] == '(')
				li++;

			Dictionary<string, object> result = new Dictionary<string, object>();

			string name = null;
			object value = null;
			while (li < line.Length)
			{
				int ei = line.IndexOf('=', li);
				if (ei == -1)
					throw new Exception(string.Format("Expected '=' after index {0}.", li));

				name = line.Substring(li, ei - li);
				li = ei + 1;

				char lc = line[li];
				if (lc == '(')
				{
					value = ParseUE4Object(line, ref li);
				}
				else if (lc == '"')
				{
					li++;
					int pi = line.IndexOf('"', li);
					if (pi == -1)
						throw new Exception(string.Format("Expected '\"' after index {0}.", li));

					value = line.Substring(li, pi - li);
					li = pi + 1;
				}
				else if (lc == 'N' && line.IndexOf("NSLOCTEXT(", li) == li)
				{
					int pi = line.IndexOf(')', li);
					if (pi == -1)
						throw new Exception(string.Format("Expected '\"' after index {0}.", li));

					value = line.Substring(li, pi - li + 1);
					li = pi + 1;
				}
				else
				{
					int ci = line.IndexOfAny(new char[] { ',', ')' }, li);
					if (ci == -1)
						throw new Exception(string.Format("Expected ',' or ')' after index {0}.", li));

					value = line.Substring(li, ci - li);
					li = ci;
				}

				lc = line[li];
				if (lc != ',' && lc != ')')
					throw new Exception(string.Format("Expected ',' or ')' at index {0}.", li));

				result.Add(name, value);
				li++;

				if (lc == ')')
					break;
			}

			return result;
		}

		private static Dictionary<string, object> ParseUE4Object(string line)
		{
			int li = 0;
			return ParseUE4Object(line, ref li);
		}

		private static string ConvertToJSON(string line)
		{
			var ue4Object = ParseUE4Object(line);

			return JsonConvert.SerializeObject(ue4Object, Newtonsoft.Json.Formatting.Indented);
		}
	}
}
