using ConsoleAppFramework;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using SystemEx;
using UE4Assistant;
using UE4Assistant.Templates.Generators;
using Template = UE4Assistant.Template;

namespace UE4AssistantCLI
{
	class Program
	{
		static async Task Main(string[] args)
		{
			AppDomain.CurrentDomain.AssemblyLoad += (object sender, AssemblyLoadEventArgs args) =>
			{
				int i = 0;
			};
			AppDomain.CurrentDomain.AssemblyResolve += (object sender, ResolveEventArgs args) =>
			{
				return null;
			};

			await Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CLI>(args);
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

		public static void AddPlugin(string pluginname)
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

		public static void AddModule(string modulename)
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

		public static void AddClass(string objectname, string basename)
		{
			AddClass(objectname, basename, true, new List<string>());
		}

		public static void AddClass(string typeName, string baseName, bool hasConstructor, List<string> extraincludes)
		{
			var typePrefix = baseName.GetTypePrefix();
			string objectfolder = Directory.GetCurrentDirectory();

			UnrealItemDescription UnrealItem = UnrealItemDescription.DetectUnrealItem(objectfolder, UnrealItemType.Module);
			if (UnrealItem == null)
			{
				Console.WriteLine("This command should be run inside module folder.");
				return;
			}

			string moduleName = UnrealItem.Name;
			string objectPath = new UnrealItemPath(UnrealItem, objectfolder).ItemPath;

			string classesPath = Path.Combine(UnrealItem.ModuleClassesPath, objectPath);
			string privatePath = Path.Combine(UnrealItem.ModulePrivatePath, objectPath);

			Directory.CreateDirectory(classesPath);
			Directory.CreateDirectory(privatePath);

			string sourceContent;
			string headerContent;
			string generatedHeader = null;
			string pchHeader = null;
			string finalHeader = null;
			string locTextNamespaceName = null;

			if (typePrefix == TypePrefix.U || typePrefix == TypePrefix.A)
			{
				generatedHeader = $"{typeName}.generated.h";
				headerContent = Template.CreateClass_h(moduleName, typePrefix, typeName, baseName, hasConstructor);
				sourceContent = Template.CreateClass_cpp(moduleName, typePrefix, typeName, baseName, hasConstructor);
			}
			else
			{
				headerContent = Template.CreateSimpleClass_h(moduleName, typePrefix, typeName, baseName);
				sourceContent = Template.CreateSimpleClass_cpp(moduleName, typePrefix, typeName, baseName);
			}

			File.WriteAllText(Path.Combine(classesPath, typeName + ".h")
				, Template.CreateHeaderFile(headerContent
					, generatedHeader: generatedHeader));
			File.WriteAllText(Path.Combine(privatePath, typeName + ".cpp")
				, Template.CreateSourceFile(sourceContent, Path.Combine(objectPath, $"{typeName}.h")
					, pchHeader: pchHeader, finalHeader: finalHeader, locTextNamespaceName: locTextNamespaceName));
		}

		public static void AddInterface(string typeName)
		{
			string objectfolder = Directory.GetCurrentDirectory();

			UnrealItemDescription UnrealItem = UnrealItemDescription.DetectUnrealItem(objectfolder, UnrealItemType.Module);
			if (UnrealItem == null)
			{
				Console.WriteLine("This command should be run inside module folder.");
				return;
			}

			string moduleName = UnrealItem.Name;
			string objectpath = new UnrealItemPath(UnrealItem, objectfolder).ItemPath;
			string classesPath = Path.Combine(UnrealItem.ModuleClassesPath, objectpath);

			Directory.CreateDirectory(classesPath);

			File.WriteAllText(Path.Combine(classesPath, typeName + ".h")
				, Template.CreateHeaderFile(Template.CreateInterface_h(moduleName, typeName)
					, generatedHeader: $"{typeName}.generated.h"));
		}

		public static void AddDataAsset(string typeName, string baseName)
		{
			string objectFolder = Directory.GetCurrentDirectory();

			UnrealItemDescription UnrealItem = UnrealItemDescription.DetectUnrealItem(objectFolder, UnrealItemType.Module);
			if (UnrealItem == null)
			{
				Console.WriteLine("This command should be run inside module folder.");
				return;
			}

			string moduleName = UnrealItem.Name;
			string objectPath = new UnrealItemPath(UnrealItem, objectFolder).ItemPath;
			string classesPath = Path.Combine(UnrealItem.ModuleClassesPath, objectPath);

			Directory.CreateDirectory(classesPath);

			File.WriteAllText(Path.Combine(classesPath, typeName + ".h")
				, Template.CreateHeaderFile(Template.CreateClass_h(moduleName, TypePrefix.U, typeName, baseName, false)
					, generatedHeader: $"{typeName}.generated.h"));
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

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		public static async Task CookProject(UnrealCookSettings[] CookSettings)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
		{
			UnrealItemDescription UnrealItem = UnrealItemDescription.DetectUnrealItem(Directory.GetCurrentDirectory(), UnrealItemType.Project);
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
}
