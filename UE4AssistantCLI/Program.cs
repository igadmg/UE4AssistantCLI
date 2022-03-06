using ConsoleAppFramework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using SystemEx;
using SystemEx.Sleep;
using UE4Assistant;
using Template = UE4Assistant.Template;

namespace UE4AssistantCLI
{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
	class Program
	{
		static async Task<int> Main(string[] args)
		{
			AppDomain.CurrentDomain.AssemblyLoad += (object sender, AssemblyLoadEventArgs args) => {
			};
			AppDomain.CurrentDomain.AssemblyResolve += (object sender, ResolveEventArgs args) => {
				return null;
			};

			try
			{
				await ConsoleApp.CreateBuilder(args)
					.ConfigureServices((ctx, services) => {
					})
					.Build()
					.AddCommands<CLI>()
					.AddSubCommands<CLI.Add>()
					.AddSubCommands<CLI.Uuid>()
					.RunAsync();
			}
			catch (UE4AssistantException e)
			{
				Console.Error.WriteLine(e.Message);
				return e.errorCode;
			}
			catch (Exception e)
			{
				Console.Error.WriteLine(e.Message);
				return -1;
			}

			return 0;
		}

		static IDisposable GenerateOnAdd(string path) => GenerateOnAdd(UnrealItemDescription.DetectUnrealItem(path, UnrealItemType.Project));
		static IDisposable GenerateOnAdd(UnrealItemDescription UnrealItem)
		{
			if (UnrealItem?.ReadConfiguration<ProjectConfiguration>()?.GenerateProject.onEditor ?? false)
			{
				return DisposableLock.Lock(() => GenerateProject(UnrealItem.RootPath));
			}

			return DisposableLock.empty;
		}

		public static IEnumerable<UnrealItemDescription> ListUnrealPlugins(string path)
		{
			foreach (string file in Directory.GetFiles(path, "*.uplugin", SearchOption.AllDirectories))
			{
				yield return new UnrealItemDescription(UnrealItemType.Plugin, file);
			}

			yield break;
		}

		public static void AddProject(string projectname)
		{
			Utilities.ExecuteCommandLine("git init");

			UProject project = Template.CreateProject(projectname);
		}

		public static void AddPlugin(string path, string pluginname)
		{
			UnrealItemDescription UnrealItem = UnrealItemDescription.RequireUnrealItem(path, UnrealItemType.Project);

			using var GenerateOnAddGuard = GenerateOnAdd(UnrealItem);
			string pluginDirectory = Path.Combine(UnrealItem.RootPath, "Plugins", pluginname);
			if (Directory.Exists(pluginDirectory))
			{
				throw new UE4AssistantException($"\"{pluginDirectory}\" should not exist.");
			}

			Directory.CreateDirectory(pluginDirectory);
			UPlugin plugin = new UPlugin();
			plugin.Save(Path.Combine(pluginDirectory, pluginname + ".uplugin"), JsonIndentation.ReadFromSettings(path));

			AddModule(pluginDirectory, pluginname);
		}

		public static void AddModule(string path, string modulename)
		{
			UnrealItemDescription UnrealItem = UnrealItemDescription.RequireUnrealItem(path, UnrealItemType.Project, UnrealItemType.Plugin);

			using var GenerateOnAddGuard = GenerateOnAdd(UnrealItem);
			string itemFullPath = UnrealItem.FullPath;
			if (UnrealItem.Type == UnrealItemType.Plugin)
			{
				var plugin = UPlugin.Load(itemFullPath);

				var module = new UModuleItem();
				module.Name = modulename;
				Template.CreateModule(plugin.RootPath, module.Name);
				plugin.Modules.Add(module);

				plugin.Save(itemFullPath, JsonIndentation.ReadFromSettings(path));
			}
			else if (UnrealItem.Type == UnrealItemType.Project)
			{
				var project = UProject.Load(itemFullPath);

				var module = new UModuleItem();
				module.Name = modulename;
				Template.CreateModule(project.RootPath, module.Name);
				project.Modules.Add(module);

				project.Save(itemFullPath, JsonIndentation.ReadFromSettings(path));
			}
		}

		public static void AddClass(string path, string typeName, string baseName, bool hasConstructor = true, string[] headers = null)
		{
			using var GenerateOnAddGuard = GenerateOnAdd(path);

			Template.CreateClass(path, typeName, baseName, hasConstructor, headers);
		}

		public static void AddBpfl(string path, string name = null)
		{
			UnrealItemDescription UnrealItem = UnrealItemDescription.RequireUnrealItem(path, UnrealItemType.Module);

			using var GenerateOnAddGuard = GenerateOnAdd(UnrealItem);
			var ProjectConfiguration = UnrealItemDescription.DetectUnrealItem(path, UnrealItemType.Project)?.ReadConfiguration<ProjectConfiguration>();
			var FunctionLibrarySuffix = ProjectConfiguration?.FunctionLibrarySuffix ?? "Statics";

			string functionlibraryname = name.IsNullOrWhiteSpace()
				? Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(UnrealItem.ItemFileName))
				: name;

			if (!functionlibraryname.EndsWith(FunctionLibrarySuffix))
				functionlibraryname += FunctionLibrarySuffix;

			AddClass(path, functionlibraryname, "UBlueprintFunctionLibrary"
				, hasConstructor: false
				, headers: new string[]
					{
						"Kismet/BlueprintFunctionLibrary.h"
					});
		}

		public static void AddInterface(string path, string typeName)
		{
			using var GenerateOnAddGuard = GenerateOnAdd(path);

			var ProjectConfiguration = UnrealItemDescription.DetectUnrealItem(path, UnrealItemType.Project)?.ReadConfiguration<ProjectConfiguration>();
			var InterfaceSuffix = ProjectConfiguration?.InterfaceSuffix ?? "Interface";

			if (!typeName.EndsWith(InterfaceSuffix))
				typeName += InterfaceSuffix;

			Template.CreateInterface(path, typeName);
		}

		public static void AddDataAsset(string path, string typeName, string baseName)
		{
			using var GenerateOnAddGuard = GenerateOnAdd(path);

			Template.CreateDataAsset(path, typeName, baseName);
		}

		public static void AddTableRow(string path, string typeName, string baseName)
		{
			using var GenerateOnAddGuard = GenerateOnAdd(path);

			Template.CreateTableRow(path, typeName, baseName);
		}

		class VSProject
		{
			public string path;
			public string guid;
			public List<string> configurations = new List<string>();
		}

		public static void InitProject(string path, string suffix)
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

		public static async Task BuildProject(string path, UnrealCookSettings[] BuildSettings)
		{
			using var SleepGuard = new PreventSleepGuard();

			UnrealItemDescription UnrealItem = UnrealItemDescription.RequireUnrealItem(path, UnrealItemType.Project);

			foreach (var setting in BuildSettings)
			{
				UnrealEngineInstance UnrealInstance;
				try { UnrealInstance = new UnrealEngineInstance(setting.UE4RootPath); }
				catch { UnrealInstance = new UnrealEngineInstance(UnrealItem); }

				setting.UE4RootPath = Path.GetFullPath(UnrealInstance.RootPath);
				setting.ProjectFullPath = Path.GetFullPath(UnrealItem.FullPath);

				var error = Utilities.ExecuteCommandLine(string.Format("\"{0}\" BuildCookRun {1}", UnrealInstance.RunUATPath, setting));
				if (error != 0)
					throw new ExecuteCommandLineException(error);
			}
		}

		public static async Task CookProject(string path, UnrealCookSettings[] CookSettings)
		{
			using var SleepGuard = new PreventSleepGuard();

			UnrealItemDescription UnrealItem = UnrealItemDescription.RequireUnrealItem(path, UnrealItemType.Project);

			foreach (var setting in CookSettings)
			{
				UnrealEngineInstance UnrealInstance;
				try { UnrealInstance = new UnrealEngineInstance(setting.UE4RootPath); }
				catch { UnrealInstance = new UnrealEngineInstance(UnrealItem); }

				if (string.IsNullOrWhiteSpace(setting.ProjectFullPath))
				{
					setting.ProjectFullPath = UnrealItem.FullPath;
				}
				setting.UE4RootPath = Path.GetFullPath(UnrealInstance.RootPath);
				setting.ProjectFullPath = Path.GetFullPath(setting.ProjectFullPath);
				setting.ArchiveDirectory = Path.GetFullPath(setting.ArchiveDirectory);

				Utilities.RequireExecuteCommandLine(string.Format("\"{0}\" BuildCookRun {1}", UnrealInstance.RunUATPath, setting));
			}
		}

		public static void GenerateProject(string path)
		{
			UnrealItemDescription UnrealItem = UnrealItemDescription.RequireUnrealItem(path, UnrealItemType.Project);

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				string UnrealVersionSelector = UnrealEngineInstance.GetUEVersionSelectorPath();
				Console.Out.WriteLine(UnrealVersionSelector);
				Utilities.RequireExecuteCommandLine(Utilities.EscapeCommandLineArgs(UnrealVersionSelector, "/projectfiles", UnrealItem.FullPath));
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				UnrealEngineInstance UnrealInstance = new UnrealEngineInstance(UnrealItem);
				Utilities.RequireExecuteCommandLine(string.Join(" "
					, "\"{0}\"".format(UnrealInstance.GenerateProjectFiles)
					, "-project=\"{0}\"".format(UnrealItem.FullPath)
					, "-game"));
			}
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

		public static string ConvertToJSON(string line)
		{
			var ue4Object = ParseUE4Object(line);

			return JsonConvert.SerializeObject(ue4Object, Newtonsoft.Json.Formatting.Indented);
		}
	}
	#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}
