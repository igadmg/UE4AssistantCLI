﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using UE4Assistant.Templates;
using UE4Assistant.Templates.Config;
using UE4Assistant.Templates.Generators;
using UE4Assistant.Templates.Source;
using UE4Assistant.Templates.Source.GameMode;
using UE4Assistant.Templates.Source.GameState;
using UE4Assistant.Templates.Source.PlayerState;



namespace UE4Assistant
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
			return new UnrealCookSettings {
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
			if (args.Length == 0)
			{
				PrintUsage();
				return -1;
			}

			if (args[0] == "add")
			{
				string type = args[1];

				if (type == "project")
				{
					AddProject(args[2]);
				}
				if (type == "plugin")
				{
					AddPlugin(args[2]);
				}
				else if (type == "module")
				{
					AddModule(args[2]);
				}
				else if (type == "class")
				{
					AddClass(args[2], args[3]);
				}
				else if (type == "bpfl")
				{
					UnrealItemDescription UnrealItem = UnrealItemDescription.DetectUnrealModule(System.IO.Directory.GetCurrentDirectory());
					if (UnrealItem == null)
					{
						Console.WriteLine("This command should be run inside module folder.");
						return -1;
					}

					string functionlibraryname = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(UnrealItem.ItemFileName)) + "Statics";

					if (File.Exists(Path.Combine(UnrealItem.ModuleClassesPath, functionlibraryname + ".h")))
					{
						Console.WriteLine("This module already contains function library.");
						return -1;
					}

					AddClass(functionlibraryname, "UBlueprintFunctionLibrary");
				}
				else if (type == "interface")
				{
					if (!args[2].EndsWith("Interface"))
						args[2] = args[2] + "Interface";
					AddInterface(args[2]);
				}
				else if (type == "dataasset")
				{
					AddDataAsset(args[2], args.Length < 4 ? "UDataAsset" : args[3]);
				}
			}
			else if (args[0] == "init")
			{
				UnrealItemDescription UnrealItem = UnrealItemDescription.DetectUnrealProject(System.IO.Directory.GetCurrentDirectory());

				if (UnrealItem == null)
				{
					Console.WriteLine("Command should be run inside project folder.");
				}
				else
				{
					InitProject(UnrealItem.RootPath, args.Length > 1 ? args[1] : string.Empty);
				}
			}
			else if (args[0] == "clean")
			{
				UnrealItemDescription UnrealItem = UnrealItemDescription.DetectUnrealProject(System.IO.Directory.GetCurrentDirectory());

				if (UnrealItem == null)
				{
					Console.WriteLine("Command should be run inside project folder.");
				}
				else
				{
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
			}
			else if (args[0] == "generate")
			{
				UnrealItemDescription UnrealItem = UnrealItemDescription.DetectUnrealProject(System.IO.Directory.GetCurrentDirectory());

				if (UnrealItem == null)
				{
					Console.WriteLine("Command should be run inside project folder.");
				}
				else
				{
					string UnrealVersionSelector = GetUEVersionSelectorPath();
					Console.Out.WriteLine(UnrealVersionSelector);
					Utilities.ExecuteCommandLine(Utilities.EscapeCommandLineArgs(UnrealVersionSelector) + " /projectfiles " + UnrealItem.FullPath);
				}
			}
			else if (args[0] == "get_ue_root")
			{
				string projectName = args.Length > 1 ? args[1] : null;
				UnrealItemDescription UnrealItem = UnrealItemDescription.DetectUnrealProject(System.IO.Directory.GetCurrentDirectory(), projectName);

				if (UnrealItem == null)
				{
					Console.WriteLine("Command should be run inside project folder.");
				}
				else
				{
					string UERoot = GetUERootForProject(UnrealItem);
					if (string.IsNullOrEmpty(UERoot))
						return -1;

					Console.WriteLine(UERoot);
					return 0;
				}
			}
			else if (args[0] == "build")
			{
				UnrealItemDescription UnrealItem = UnrealItemDescription.DetectUnrealProject(System.IO.Directory.GetCurrentDirectory());
				UnrealCookSettings BuildSettings = UnrealCookSettings.CreateBuildSettings();

				using (var SleepGuard = Utilities.PreventSleep())
				{
					BuildSettings.UE4RootPath = Path.GetFullPath(GetUERootForProject(UnrealItem));
					BuildSettings.ProjectFullPath = Path.GetFullPath(UnrealItem.FullPath);					

					string RunUATPath = Path.Combine(BuildSettings.UE4RootPath, "Engine\\Build\\BatchFiles\\RunUAT.bat");

					Utilities.ExecuteCommandLine(string.Format("\"{0}\" BuildCookRun {1}", RunUATPath, BuildSettings));
				}
			}
			else if (args[0] == "cook")
			{
				string cookSettingsFile = args.Length > 1 ? args[1] : null;
				UnrealItemDescription UnrealItem = UnrealItemDescription.DetectUnrealProject(System.IO.Directory.GetCurrentDirectory());
				UnrealCookSettings[] CookSettings = cookSettingsFile != null
					? JsonConvert.DeserializeObject<UnrealCookSettings[]>(File.ReadAllText(cookSettingsFile)
						, new JsonSerializerSettings
						{
							ObjectCreationHandling = ObjectCreationHandling.Replace
						})
					: new UnrealCookSettings[] { UnrealCookSettings.CreateDefaultSettings() };

				using (var SleepGuard = Utilities.PreventSleep())
				{
					foreach (var Recipe in CookSettings)
					{
						if (string.IsNullOrWhiteSpace(Recipe.UE4RootPath))
						{
							Recipe.UE4RootPath = GetUERootForProject(UnrealItem);
						}
						if (string.IsNullOrWhiteSpace(Recipe.ProjectFullPath))
						{
							Recipe.ProjectFullPath = UnrealItem.FullPath;
						}
						Recipe.UE4RootPath = Path.GetFullPath(Recipe.UE4RootPath);
						Recipe.ProjectFullPath = Path.GetFullPath(Recipe.ProjectFullPath);
						Recipe.ArchiveDirectory = Path.GetFullPath(Recipe.ArchiveDirectory);

						string RunUATPath = Path.Combine(Recipe.UE4RootPath, "Engine\\Build\\BatchFiles\\RunUAT.bat");

						Utilities.ExecuteCommandLine(string.Format("\"{0}\" BuildCookRun {1}", RunUATPath, Recipe));
					}
				}
			}
			else if (args[0] == "convert")
			{
				if (args.Length == 1)
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
			}
			else if (args[0] == "merge")
			{
				if (args.Length == 2)
				{
					UnrealItemDescription UnrealItem = UnrealItemDescription.DetectUnrealProject(System.IO.Directory.GetCurrentDirectory());
					string UE4EditorPath = Path.Combine(GetUERootForProject(UnrealItem), "Engine\\Binaries\\Win64\\UE4Editor.exe");

					string savedDiffPath = Path.Combine(UnrealItem.RootPath, "Saved\\Diff");
					if (!Directory.Exists(savedDiffPath))
						Directory.CreateDirectory(savedDiffPath);

					string file = args[1];
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
						UE4EditorPath, UnrealItem.FullPath, "-diff", remoteFile, localFile, baseFile, resultFile));

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
			}

			return 0;
		}

		static IEnumerable<UnrealItemDescription> ListUnrealPlugins(string path)
		{
			foreach (string file in System.IO.Directory.GetFiles(path, "*.uplugin", SearchOption.AllDirectories))
			{
				yield return new UnrealItemDescription(UnrealItemType.Plugin, file);
			}

			yield break;
		}

		private static void AddProject(string projectname)
		{
			Utilities.ExecuteCommandLine("git init");

			UProject project = new UProject();

			string configPath = Path.Combine("Config");
			string contentPath = Path.Combine("Content");
			string sourcePath = Path.Combine("Source/Game");

			Directory.CreateDirectory(configPath);
			Directory.CreateDirectory(contentPath);
			Directory.CreateDirectory(sourcePath);

			{
				UModule module = new UModule();
				module.Name = projectname;
				module.Type = "Runtime";
				module.LoadingPhase = "Default";
				project.AddModule(module);

				Dictionary<string, object> parameters = new Dictionary<string, object>
				{
					{ "targetname", module.Name },
					{ "targettype", "Game" },
					{ "extramodulenames", new List<String> { module.Name } },
				};

				File.WriteAllText(Path.Combine(sourcePath, projectname + ".Target.cs")
					, Template.TransformToText<ProjectTarget_cs>(parameters));

				{
					parameters.Add("modulename", module.Name);
					parameters.Add("isprimary", true);

					string modulePath = Path.Combine(sourcePath, module.Name);
					string gamemodePath = Path.Combine(modulePath, "GameMode");
					string gamestatePath = Path.Combine(modulePath, "GameState");
					string playerstatePath = Path.Combine(modulePath, "PlayerState");

					Directory.CreateDirectory(modulePath);
					Directory.CreateDirectory(gamemodePath);
					Directory.CreateDirectory(gamestatePath);
					Directory.CreateDirectory(playerstatePath);

					File.WriteAllText(Path.Combine(gamemodePath, module.Name + "GameMode.h")
						, Template.TransformToText<GameMode_h>(parameters));
					File.WriteAllText(Path.Combine(gamemodePath, module.Name + "GameMode.cpp")
						, Template.TransformToText<GameMode_cpp>(parameters));

					File.WriteAllText(Path.Combine(gamestatePath, module.Name + "GameState.h")
						, Template.TransformToText<GameState_h>(parameters));
					File.WriteAllText(Path.Combine(gamestatePath, module.Name + "GameState.cpp")
						, Template.TransformToText<GameState_cpp>(parameters));

					File.WriteAllText(Path.Combine(playerstatePath, module.Name + "PlayerState.h")
						, Template.TransformToText<PlayerState_h>(parameters));
					File.WriteAllText(Path.Combine(playerstatePath, module.Name + "PlayerState.cpp")
						, Template.TransformToText<PlayerState_cpp>(parameters));

					File.WriteAllText(Path.Combine(modulePath, module.Name + "GameInstance.h")
						, Template.TransformToText<GameInstance_h>(parameters));
					File.WriteAllText(Path.Combine(modulePath, module.Name + "GameInstance.cpp")
						, Template.TransformToText<GameInstance_cpp>(parameters));

					File.WriteAllText(Path.Combine(modulePath, module.Name + "Statics.h")
						, Template.TransformToText<Statics_h>(parameters));
					File.WriteAllText(Path.Combine(modulePath, module.Name + "Statics.cpp")
						, Template.TransformToText<Statics_cpp>(parameters));
				}
			}

			{
				UModule module = new UModule();
				module.Name = projectname + "Editor";
				module.Type = "Editor";
				module.LoadingPhase = "Default";
				project.AddModule(module);

				Dictionary<string, object> parameters = new Dictionary<string, object>
				{
					{ "targetname", module.Name },
					{ "targettype", "Editor" },
					{ "extramodulenames", new List<String> { projectname } }
				};

				File.WriteAllText(Path.Combine(sourcePath, projectname + "Editor.Target.cs")
					, Template.TransformToText<ProjectTarget_cs>(parameters));
			}

			project.Save(projectname + ".uproject");

			{
				Dictionary<string, object> parameters = new Dictionary<string, object>
				{
					{ "modulename", projectname }
				};

				File.WriteAllText(Path.Combine(configPath, "DefaultEditor.ini")
					, Template.TransformToText<DefaultEditor_ini>(parameters));
				File.WriteAllText(Path.Combine(configPath, "DefaultEditorSettings.ini")
					, Template.TransformToText<DefaultEditorSettings_ini>(parameters));
				File.WriteAllText(Path.Combine(configPath, "DefaultEngine.ini")
					, Template.TransformToText<DefaultEngine_ini>(parameters));
				File.WriteAllText(Path.Combine(configPath, "DefaultGame.ini")
					, Template.TransformToText<DefaultGame_ini>(parameters));
				File.WriteAllText(Path.Combine(configPath, "DefaultGameUserSettings.ini")
					, Template.TransformToText<DefaultGameUserSettings_ini>(parameters));
				File.WriteAllText(Path.Combine(configPath, "DefaultInput.ini")
					, Template.TransformToText<DefaultInput_ini>(parameters));
			}

			/*
			foreach (string path in Utilities.ListSubmodulePaths("."))
			{
				Utilities.ExecuteCommandLine(string.Format("cd {0} && git checkout master"
					, Utilities.EscapeCommandLineArgs(path)));
			}
			*/
		}

		private static void AddPlugin(string pluginname)
		{
			UPlugin plugin = new UPlugin();

			{
				UModule module = new UModule();
				module.Name = pluginname;
				module.Type = "Runtime";
				module.LoadingPhase = "Default";
				plugin.AddModule(module);
			}

			plugin.Save(pluginname + ".uplugin");
		}

		private static void AddModule(string modulename)
		{
			UnrealItemDescription UnrealItem = UnrealItemDescription.DetectUnrealProject(System.IO.Directory.GetCurrentDirectory());
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
					module.Type = "Runtime";
					module.LoadingPhase = "Default";
					plugin.AddModule(module);

					plugin.Save(itemFullPath);
				}
				else if (UnrealItem.Type == UnrealItemType.Project)
				{
					UProject project = UProject.Load(itemFullPath);

					UModule module = new UModule();
					module.Name = modulename;
					module.Type = "Runtime";
					module.LoadingPhase = "Default";
					project.AddModule(module);

					project.Save(itemFullPath);
				}
			}
		}

		private static void AddClass(string objectname, string basename)
		{
			string typename = basename[0].ToString();
			string objectfolder = System.IO.Directory.GetCurrentDirectory();

			UnrealItemDescription UnrealItem = UnrealItemDescription.DetectUnrealModule(objectfolder);
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
					{ "objectpath", objectpath.Replace('\\', '/') },
					{ "basename", basename },
					{ "typename", typename },
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
			string objectfolder = System.IO.Directory.GetCurrentDirectory();

			UnrealItemDescription UnrealItem = UnrealItemDescription.DetectUnrealModule(objectfolder);
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
					{ "objectpath", objectpath.Replace('\\', '/') },
				};

			string classesPath = Path.Combine(UnrealItem.ModuleClassesPath, objectpath);

			Directory.CreateDirectory(classesPath);

			File.WriteAllText(Path.Combine(classesPath, objectname + ".h")
				, Template.TransformToText<Interface_h>(parameters));
		}

		private static void AddDataAsset(string objectname, string basename)
		{
			string objectfolder = System.IO.Directory.GetCurrentDirectory();

			UnrealItemDescription UnrealItem = UnrealItemDescription.DetectUnrealModule(objectfolder);
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
					{ "objectpath", objectpath.Replace('\\', '/') },
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

		private static string GetUEVersionSelectorPath()
		{
			var LocalUnrealEngine = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(@"Unreal.ProjectFile\DefaultIcon");
			if (LocalUnrealEngine != null)
			{
				return ((string)LocalUnrealEngine.GetValue("")).Trim('"', ' ');
			}

			return string.Empty;
		}

		private static string GetUERootForProject(UnrealItemDescription Description)
		{
			UProject project = UProject.Load(Description.FullPath);

			Dictionary<string, string> availableBuilds = new Dictionary<string, string>();

			var LocalUnrealEngine = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\EpicGames\Unreal Engine");
			var UserUnrealEngineBuilds = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Epic Games\Unreal Engine\Builds");

			if (LocalUnrealEngine != null)
			{
				foreach (string build in LocalUnrealEngine.GetSubKeyNames())
				{
					string ueroot = (string)LocalUnrealEngine.OpenSubKey(build).GetValue("InstalledDirectory");

					if (!string.IsNullOrWhiteSpace(ueroot))
					{
						availableBuilds.Add(build, ueroot);
					}
				}
			}

			if (UserUnrealEngineBuilds != null)
			{
				foreach (string build in UserUnrealEngineBuilds.GetValueNames())
				{
					string ueroot = (string)UserUnrealEngineBuilds.GetValue(build);

					if (!string.IsNullOrWhiteSpace(ueroot))
					{
						availableBuilds.Add(build, ueroot);
					}
				}
			}

			string projectUERoot;
			if (availableBuilds.TryGetValue(project.EngineAssociation, out projectUERoot))
			{
				return projectUERoot;
			}

			return null;
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
