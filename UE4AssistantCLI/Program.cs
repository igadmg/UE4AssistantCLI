using ANSIConsole;
using ConsoleAppFramework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using SystemEx.Sleep;

using ILogger = Microsoft.Extensions.Logging.ILogger;
using Template = UE4Assistant.Template;

namespace UE4AssistantCLI;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
class Program
{
	public class ConsoleLogger : ILogger
	{
		public class Provider : ILoggerProvider
		{
			public ILogger CreateLogger(string categoryName) => new ConsoleLogger();

			public void Dispose()
			{
			}
		}

		readonly LogLevel minimumLogLevel;

		public ConsoleLogger(LogLevel minimumLogLevel = LogLevel.Information)
			=> this.minimumLogLevel = minimumLogLevel;

		public IDisposable BeginScope<TState>(TState state) => DisposableLock.empty;
		public bool IsEnabled(LogLevel logLevel) => minimumLogLevel <= logLevel;

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
		{
			if (minimumLogLevel > logLevel) return;

			if (formatter != null)
			{
				var msg = formatter(state, exception);

				if (!string.IsNullOrEmpty(msg))
				{
					Console.WriteLine(msg);
				}
			}
			if (exception != null)
			{
				Console.Error.WriteLine(exception.Message);
				Environment.ExitCode = exception.HResult;
			}
		}
	}

	public static bool no_generate = false;

	static async Task<int> Main(string[] args)
	{
		using var ansi = new ANSIInitializer();

		//AppDomain.CurrentDomain.AssemblyLoad += (object sender, AssemblyLoadEventArgs args) => {
		//};
		//AppDomain.CurrentDomain.AssemblyResolve += (object sender, ResolveEventArgs args) => {
		//	return null;
		//};

		await ConsoleApp.CreateFromHostBuilder(Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
				.ConfigureServices((ctx, services) => {
					services.RemoveAll<ILoggerProvider>();
					services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, ConsoleLogger.Provider>());
				})
			, args, options => {
				options.StrictOption = false;
				options.HelpSortCommandsByFullName = true;
			})
			.AddCommands<CLI>()
			.AddSubCommands<CLI.Add>()
			.AddSubCommands<CLI.Config>()
			.AddSubCommands<CLI.Fix>()
			.AddSubCommands<CLI.Log>()
			.AddSubCommands<CLI.Uuid>()
			.RunAsync();

		return Environment.ExitCode;
	}

	static IDisposable GenerateOnAdd(string path) => GenerateOnAdd(UnrealItemDescription.DetectUnrealItem(path, UnrealItemType.Project));
	static IDisposable GenerateOnAdd(UnrealItemDescription UnrealItem)
	{
		if (no_generate)
			return DisposableLock.empty;

		if (UnrealItem?.ReadConfiguration<ProjectConfiguration>()?.GenerateProject.onAddItem ?? false)
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
		Utilities.RequireExecuteCommandLine("git init");

		UProject project = Template.CreateProject(projectname);
	}

	public static void AddPlugin(string path, params string[] pluginnames)
	{
		UnrealItemDescription UnrealItem = UnrealItemDescription.RequireUnrealItem(path, UnrealItemType.Project);

		using var GenerateOnAddGuard = GenerateOnAdd(UnrealItem);

		foreach (var pluginname in pluginnames)
		{
			UPlugin plugin = null;
			string pluginDirectory = Path.Combine(UnrealItem.RootPath, "Plugins", pluginname);
			if (!Directory.Exists(pluginDirectory))
			{
				Directory.CreateDirectory(pluginDirectory);
				plugin = new UPlugin(pluginname);
				plugin.RootPath = pluginDirectory;
				plugin.Save(JsonIndentation.ReadFromSettings(path));

				AddModule(pluginDirectory, pluginname);
			}
			else
			{
				plugin = UPlugin.Load(Directory.GetFiles(pluginDirectory, "*.uplugin").First());
			}

			var project = UProject.Load(UnrealItem.FullPath);

			var pi = project.Plugins.Find(x => x.Name == plugin.Name)
				?? new UPluginItem { Name = plugin.Name }.Also(_ => project.Plugins.Add(_));
			pi.Enabled = true;
			project.Save(JsonIndentation.ReadFromSettings(path));
		}
	}

	public static void AddModule(string path, params string[] modulenames)
	{
		UnrealItemDescription UnrealItem = UnrealItemDescription.RequireUnrealItem(path, UnrealItemType.Project, UnrealItemType.Plugin);

		using var GenerateOnAddGuard = GenerateOnAdd(UnrealItem);

		foreach (var modulename in modulenames)
		{
			if (UnrealItem.Type == UnrealItemType.Plugin)
			{
				var plugin = UPlugin.Load(UnrealItem.FullPath);

				var module = new UModuleItem();
				module.Name = modulename;
				Template.CreateModule(plugin.RootPath, module.Name);
				plugin.Modules.Add(module);

				plugin.Save(JsonIndentation.ReadFromSettings(path));
			}
			else if (UnrealItem.Type == UnrealItemType.Project)
			{
				var project = UProject.Load(UnrealItem.FullPath);

				var module = new UModuleItem();
				module.Name = modulename;
				Template.CreateModule(project.RootPath, module.Name);
				project.Modules.Add(module);

				project.Save(JsonIndentation.ReadFromSettings(path));
			}
		}
	}

	public static void SanitizePathAndName(ref string path, ref string name)
	{
		var fullPath = Path.Combine(path, name);
		path = Path.GetDirectoryName(fullPath);
		name = Path.GetFileName(fullPath);

		if (!Directory.Exists(path))
			Directory.CreateDirectory(path);
	}

	public static void AddClass(string path, string name, string baseName, bool hasConstructor = true, string[] headers = null)
	{
		SanitizePathAndName(ref path, ref name);
		using var GenerateOnAddGuard = GenerateOnAdd(path);

		Template.CreateClass(path, name, baseName, hasConstructor, headers);
	}

	public static void AddBpfl(string path, string name = null)
	{
		SanitizePathAndName(ref path, ref name);
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

	public static void AddInterface(string path, string name)
	{
		SanitizePathAndName(ref path, ref name);
		using var GenerateOnAddGuard = GenerateOnAdd(path);

		var ProjectConfiguration = UnrealItemDescription.DetectUnrealItem(path, UnrealItemType.Project)?.ReadConfiguration<ProjectConfiguration>();
		var InterfaceSuffix = ProjectConfiguration?.InterfaceSuffix ?? "Interface";

		if (!name.EndsWith(InterfaceSuffix))
			name += InterfaceSuffix;

		Template.CreateInterface(path, name);
	}

	public static void AddDataAsset(string path, string name, string baseName)
	{
		SanitizePathAndName(ref path, ref name);
		using var GenerateOnAddGuard = GenerateOnAdd(path);

		Template.CreateDataAsset(path, name, baseName);
	}

	public static void AddTableRow(string path, string name, string baseName)
	{
		SanitizePathAndName(ref path, ref name);
		using var GenerateOnAddGuard = GenerateOnAdd(path);

		Template.CreateTableRow(path, name, baseName);
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

	public static async Task BuildProject(string path, UnrealCookSettings[] BuildSettings, Action<string> contentFn = null)
	{
		using var SleepGuard = new PreventSleepGuard();

		UnrealItemDescription UnrealItem = UnrealItemDescription.RequireUnrealItem(path, UnrealItemType.Project);

		foreach (var setting in BuildSettings.Select(s => UnrealItem.SanitizeSettings(s)))
		{
			UnrealEngineInstance UnrealInstance;
			try { UnrealInstance = new UnrealEngineInstance(setting.UE4RootPath); }
			catch { UnrealInstance = new UnrealEngineInstance(UnrealItem); }

			setting.UE4RootPath = Path.GetFullPath(UnrealInstance.RootPath);
			setting.ProjectFullPath = Path.GetFullPath(UnrealItem.FullPath);

			UnrealInstance.Setup();
			Utilities.RequireExecuteCommandLine($@"""{UnrealInstance.RunUATSh}"" BuildCookRun {setting}", contentFn);
		}
	}

	public static async Task BuildEngine(string path, Action<string> contentFn = null)
	{
		using var SleepGuard = new PreventSleepGuard();

		UnrealItemDescription ui = UnrealItemDescription.RequireUnrealItem(path, UnrealItemType.Engine);
		var uei = new UnrealEngineInstance(ui.RootPath);

		uei.Setup();
		Utilities.RequireExecuteCommandLine(
			$@"""{uei.RunUATSh}"" BuildGraph -target=""Build Tools {UnrealCookSettings.DefaultPlatformName}"" -script={uei.BuildPath.AsLinuxPath()}/InstalledEngineBuild.xml", contentFn);
		Utilities.RequireExecuteCommandLine(
			$@"""{uei.RunUBTSh}"" {uei.EditorBuildTarget} {UnrealCookSettings.DefaultPlatformName} Development", contentFn);
	}

	public static async Task CookProject(string path, UnrealCookSettings[] CookSettings, Action<string> contentFn = null)
	{
		using var SleepGuard = new PreventSleepGuard();

		UnrealItemDescription UnrealItem = UnrealItemDescription.RequireUnrealItem(path, UnrealItemType.Project);

		foreach (var setting in CookSettings.Select(s => UnrealItem.SanitizeSettings(s)))
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

			UnrealInstance.Setup();
			Utilities.RequireExecuteCommandLine($@"""{UnrealInstance.RunUATSh}"" BuildCookRun {setting}", contentFn);
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
