using ClipboardEx.Win32;
using ConsoleAppFramework;
using System.IO;
using System.Threading.Tasks;

namespace UE4AssistantCLI;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
class CLI : ConsoleAppBase
{
	[Command("add")]
	public class Add : ConsoleAppBase
	{
		[Option("ng", "skip project generation step")]
		bool no_generate = false;

		[Command("project", "Create new project.")]
		public async Task AddProject([Option(0, "project name")] string name)
		{
			Program.no_generate = no_generate;
			Program.AddProject(name);
		}

		[Command("plugin", "Create and add new plugin to current project.")]
		public async Task AddPlugin([Option(0, "plugin names")] params string[] names)
		{
			Program.no_generate = no_generate;
			Program.AddPlugin(Directory.GetCurrentDirectory(), names);
		}

		[Command("module", "Add new module to current project or plugin.")]
		public async Task AddModule([Option(0, "module names")] params string[] names)
		{
			Program.no_generate = no_generate;
			Program.AddModule(Directory.GetCurrentDirectory(), names);
		}

		[Command("class", "Add new class to current module.")]
		public async Task AddClass([Option(0, "class name")] string name, [Option(1, "base class name")] string basename = "UObject")
		{
			Program.no_generate = no_generate;
			Program.AddClass(Directory.GetCurrentDirectory(), name, basename);
		}

		[Command("bpfl", "Add a function library to current module, if one does not exist.")]
		public async Task AddBpfl([Option(0, "class name")] string name = null)
		{
			Program.no_generate = no_generate;
			Program.AddBpfl(Directory.GetCurrentDirectory(), name);
		}

		[Command("interface", "Add new interface to current module.")]
		public async Task AddInterface([Option(0, "interface name")] string name)
		{
			Program.no_generate = no_generate;
			Program.AddInterface(Directory.GetCurrentDirectory(), name);
		}

		[Command("dataasset", "Add a data asset class with given name.")]
		public async Task AddDataAsset([Option(0, "data asset name")] string name, [Option(1, "data asset base class name")] string basename = "UDataAsset")
		{
			Program.no_generate = no_generate;
			Program.AddDataAsset(Directory.GetCurrentDirectory(), name, basename);
		}

		[Command("tablerow", "Add a data table row struct with given name.")]
		public async Task AddTableRow([Option(0, "table row name")] string name, [Option(1, "table row base class name")] string basename = "FTableRowBase")
		{
			Program.no_generate = no_generate;
			Program.AddTableRow(Directory.GetCurrentDirectory(), name, basename);
		}
	}

	[Command("config")]
	public class Config : ConsoleAppBase
	{
		string GetConfigFileName(string path, string configFile)
		{
			if (File.Exists(configFile)) return configFile;

			var f = Path.Combine(path, configFile);
			if (File.Exists(f)) return f;

			var UnrealItem = UnrealItemDescription.RequireUnrealItem(path, UnrealItemType.Engine, UnrealItemType.Project, UnrealItemType.Game);
			f = Path.Combine(UnrealItem.RootPath, configFile);
			if (File.Exists(f)) return f;

			foreach (var ConfigurationPath in UnrealItem.ConfigurationPaths)
			{
				f = Path.Combine(ConfigurationPath, configFile);
				if (File.Exists(f)) return f;

				f = Path.Combine(ConfigurationPath, configFile + ".ini");
				if (File.Exists(f)) return f;

				f = Path.Combine(ConfigurationPath, "Default" + configFile);
				if (File.Exists(f)) return f;

				f = Path.Combine(ConfigurationPath, "Default" + configFile + ".ini");
				if (File.Exists(f)) return f;
			}

			return null;
		}

		[Command("get", "Get value from config file.")]
		public async Task Get([Option(0)] string configFile, [Option(1)] string section, [Option(2)] string key)
		{
			string filename = GetConfigFileName(Directory.GetCurrentDirectory(), configFile);

			if (filename.IsNullOrWhiteSpace())
				throw new UE4AssistantException($"Can't find configuration file '{configFile}'.");

			var config = new ConfigFile(filename);

			var s = config[section];
			if (s == null) throw new UE4AssistantException($"Can't find section '{section}' in file '{filename}'.");

			var v = s.lines.Find(l => l.key == key);
			if (v.key.IsNullOrWhiteSpace()) throw new UE4AssistantException($"Can't find key '{key}' in section '{section}' in file '{filename}'.");

			Console.WriteLine(v.value);
		}

		[Command("set", "Set value in config file.")]
		public async Task Set([Option(0)] string configFile, [Option(1)] string section, [Option(2)] string key, [Option(3)] string value = null)
		{
			if (value == null && Console.In.Peek() != -1)
				value = Console.ReadLine();

			string filename = GetConfigFileName(Directory.GetCurrentDirectory(), configFile);

			if (filename.IsNullOrWhiteSpace())
				throw new UE4AssistantException($"Can't find configuration file '{configFile}'.");

			var config = new ConfigFile(filename);

			var s = config[section] ?? config.AddSection(section);
			s[key] = value;

			config.Save(filename);
		}
	}

	[Command("fix")]
	public class Fix : ConsoleAppBase
	{
		[Command("all", "Fix all possible errors.")]
		public async Task All()
		{
			await DllLoad();
			await PCHCleanup();
		}

		[Command("dll_load", "Fix `Failed to load dll` errors.")]
		public async Task DllLoad()
		{
			var UnrealItem = UnrealItemDescription.RequireUnrealItem(Directory.GetCurrentDirectory(), UnrealItemType.Project);

			Console.WriteLine($"==> {UnrealItem.ProjectLogFile}");
			var dllLoadFiles = new HashSet<string>(Utilities.ParseDllLoadErrors(File.ReadAllText(UnrealItem.ProjectLogFile)));

			var rootPath = UnrealItem.RootPath.AsLinuxPath();
			foreach (var dllFile in dllLoadFiles.Select(f => f.AsLinuxPath()))
			{
				if (!dllFile.StartsWith(rootPath))
					continue;

				Console.WriteLine(dllFile);
				File.Delete(dllFile);
			}
		}

		[Command("pch_cleanup", "Cleanup PCH file errors.")]
		public async Task PCHCleanup()
		{
			var UnrealItem = UnrealItemDescription.RequireUnrealItem(Directory.GetCurrentDirectory(), UnrealItemType.Project);

			foreach (var buildLog in UnrealItem.BuildLogs)
			{
				Console.WriteLine($"==> {buildLog}");
				var pchFiles = new HashSet<string>(Utilities.ParsePCHFilesErrors(File.ReadAllText(buildLog)));

				foreach (var pchFile in pchFiles)
				{
					Console.WriteLine(pchFiles);
					File.Delete(pchFile);
				}
			}
		}
	}

	[Command("log")]
	public class Log : ConsoleAppBase
	{
		[Command("project", "Open project log folder.")]
		public async Task ProjectLog()
		{
			var UnrealItem = UnrealItemDescription.RequireUnrealItem(Directory.GetCurrentDirectory(), UnrealItemType.Project);

			Utilities.ExecuteOpenFile(UnrealItem.ProjectLogPath);
		}

		[Command("build", "Open build log file.")]
		public async Task BuildLog()
		{
			var UnrealItem = UnrealItemDescription.RequireUnrealItem(Directory.GetCurrentDirectory(), UnrealItemType.Project);

			Utilities.ExecuteOpenFile(UnrealItem.BuildLogPath);
		}
	}

	[Command("uuid")]
	public class Uuid : ConsoleAppBase
	{
		[Command("list", "List registered Unreal Engine uuid identifiers.")]
		public async Task List()
		{
			var availableBuilds = UnrealEngineInstance.FindAvailableBuilds();

			foreach (var build in availableBuilds)
			{
				Console.WriteLine($"{build.Key}\t{build.Value}");
			}
		}

		[RootCommand]
		[Command("show", "Show project's Unreal Engine uuid identifier.")]
		public async Task Show()
		{
			var UnrealItem = UnrealItemDescription.RequireUnrealItem(Directory.GetCurrentDirectory(), UnrealItemType.Project, UnrealItemType.Engine);

			Console.WriteLine(new UnrealEngineInstance(UnrealItem).Uuid);
		}

		[Command("set", "Set project's Unreal Engine uuid identifier.")]
		public async Task Set([Option(0, "engine-uuid")] string uuid = null)
		{
			uuid ??= Guid.NewGuid().ToString();

			var UnrealItem = UnrealItemDescription.RequireUnrealItem(Directory.GetCurrentDirectory(), UnrealItemType.Project, UnrealItemType.Engine);

			try
			{
				var UnrealInstance = new UnrealEngineInstance(UnrealItem);

				var Uuid = new Guid(uuid);
				UnrealEngineInstance.SetUserUnrealEngineBuilds(Uuid.ToString("B").ToUpper(), UnrealInstance.RootPath);
			}
			catch {}

			if (UnrealItem.Type == UnrealItemType.Project)
			{
				UProject project = UProject.Load(UnrealItem.FullPath);
				project.EngineAssociation = uuid;
				project.Save(JsonIndentation.ReadFromSettings(Directory.GetCurrentDirectory()));
			}
		}
	}

	[Command("init", "Initialize working environment, create Libraries.sln.")]
	public async Task InitProject([Option(0, "UE4 version")] string UE4Version = null)
	{
		var UnrealItem = UnrealItemDescription.RequireUnrealItem(Directory.GetCurrentDirectory(), UnrealItemType.Project);
		Program.InitProject(UnrealItem.RootPath, UE4Version ?? "");
	}

	[Command("clean", "Clean project and plugins from build files.")]
	public async Task CleanProject()
	{
		var UnrealItem = UnrealItemDescription.RequireUnrealItem(Directory.GetCurrentDirectory(), UnrealItemType.Project);

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
		Program.GenerateProject(Directory.GetCurrentDirectory());
	}

	[Command("editor", "Open Unreal Engine Editor and open current project if run inside project folder.")]
	public async Task OpenEditor([Option("ng", "skip project generation step")] bool no_generate = false)
	{
		var UnrealItem = UnrealItemDescription.RequireUnrealItem(Directory.GetCurrentDirectory(), UnrealItemType.Project, UnrealItemType.Engine);

		if (UnrealItem.Type == UnrealItemType.Project)
		{
			if (!no_generate && (UnrealItem.ReadConfiguration<ProjectConfiguration>()?.GenerateProject.onEditor ?? false))
			{
				await GenerateProject();
			}

			Utilities.ExecuteOpenFile(UnrealItem.FullPath);
		}
		else if (UnrealItem.Type == UnrealItemType.Engine)
		{
			var uei = new UnrealEngineInstance(UnrealItem.RootPath);
			Utilities.ExecuteOpenFile(uei.UnrealEditorPath);
		}
	}

	[Command("code", "Open Source Code Editor for current project.")]
	public async Task OpenCode([Option("ng", "skip project generation step")] bool no_generate = false)
	{
		var UnrealItem = UnrealItemDescription.RequireUnrealItem(Directory.GetCurrentDirectory(), UnrealItemType.Project);

		if (!no_generate && (UnrealItem.ReadConfiguration<ProjectConfiguration>()?.GenerateProject.onCode ?? false))
		{
			await GenerateProject();
		}

		var solutionFile = Path.Combine(UnrealItem.RootPath, UnrealItem.Name + ".sln");
		if (!File.Exists(solutionFile))
			await GenerateProject();

		Utilities.ExecuteOpenFile(solutionFile);
	}

	[Command("run", "Run commandlet.")]
	public async Task RunCommandlet([Option(0)] string commandlet, [Option(1)] params string[] parameters)
	{
		var UnrealItem = UnrealItemDescription.RequireUnrealItem(Directory.GetCurrentDirectory(), UnrealItemType.Project);
		var UnrealInstance = new UnrealEngineInstance(UnrealItem);

		Utilities.RequireExecuteCommandLine(ProcessEx.EscapeArguments(
			new[] { UnrealInstance.UnrealCmdPath, UnrealItem.FullPath, $"-run={commandlet}" }.Concat(parameters)), str => {
				UnrealLogPrinter.WriteLine(str);
			});
	}

	[Command("get_ue_root", "Get UE root of associated UE build.")]
	public async Task GetUERoot([Option(0, "project name")] string ProjectName = null)
	{
		var UnrealItem = UnrealItemDescription.RequireUnrealItem(Directory.GetCurrentDirectory(), ProjectName ?? "", UnrealItemType.Project);

		Console.WriteLine(new UnrealEngineInstance(UnrealItem).RootPath);
	}

	T[] LoadSettings<T>(string fileName, Func<T> defaultSettings)
	{
		return !fileName.IsNullOrWhiteSpace() && File.Exists(fileName)
			? JsonConvert.DeserializeObject<T[]>(File.ReadAllText(fileName)
				, new JsonSerializerSettings {
					ObjectCreationHandling = ObjectCreationHandling.Replace
				})
			: new T[] { defaultSettings() };
	}

	[Command("build", "Build project.")]
	public async Task BuildProject([Option(0, "build settings json file name")] string BuildSettingsJson = null
		, [Option("dump", "Dump configuration file to the console.")] bool dump = false)
	{
		var UnrealItem = UnrealItemDescription.DetectUnrealItem(Directory.GetCurrentDirectory(), UnrealItemType.Project, UnrealItemType.Engine);
		var ProjectConfiguration = UnrealItem?.ReadConfiguration<ProjectConfiguration>();

		if (BuildSettingsJson.IsNullOrWhiteSpace() && !(ProjectConfiguration?.DefaultBuildConfigurationFile).IsNullOrWhiteSpace())
			BuildSettingsJson = Utilities.GetFullPath(ProjectConfiguration.DefaultBuildConfigurationFile, UnrealItem.RootPath);

		var BuildSettings = LoadSettings(BuildSettingsJson
			, () => UnrealItem.Type == UnrealItemType.Engine
				? null : UnrealCookSettings.CreateBuildSettings()
			.Also(s => {
				if (ProjectConfiguration != null) s.UE4RootPath = ProjectConfiguration.UE4RootPath;
				s.Platform ??= UnrealCookSettings.DefaultPlatformName;
			}))
			.Select(s => s?.Also(_ => _.Platform ??= UnrealCookSettings.DefaultPlatformName))
			.ToArray();

		if (dump)
		{
			Console.WriteLine(BuildSettings.SerializeObject(Formatting.Indented, JsonIndentation.ReadFromSettings(Directory.GetCurrentDirectory())));
		}
		else
		{
			if (UnrealItem.Type == UnrealItemType.Project)
				await Program.BuildProject(UnrealItem.RootPath, BuildSettings, str => {
					UnrealLogPrinter.WriteLine(str);
				});
			if (UnrealItem.Type == UnrealItemType.Engine)
				await Program.BuildEngine(UnrealItem.RootPath, str => {
					UnrealLogPrinter.WriteLine(str);
				});
		}
	}

	[Command("cook", "Cook project with cook settings.")]
	public async Task CookProject([Option(0, "cook settings json file name")] string CookSettingsJson = null
		, [Option("dump", "Dump configuration file config to console.")] bool dump = false)
	{
		var UnrealItem = UnrealItemDescription.DetectUnrealItem(Directory.GetCurrentDirectory(), UnrealItemType.Project);
		var ProjectConfiguration = UnrealItem?.ReadConfiguration<ProjectConfiguration>();

		if (CookSettingsJson.IsNullOrWhiteSpace() && !(ProjectConfiguration.DefaultCookConfigurationFile).IsNullOrWhiteSpace())
			CookSettingsJson = Utilities.GetFullPath(ProjectConfiguration.DefaultCookConfigurationFile, UnrealItem.RootPath);

		var CookSettings = LoadSettings(CookSettingsJson, () => UnrealCookSettings.CreateDefaultSettings()
			.Also(s => {
				if (ProjectConfiguration != null) s.UE4RootPath = ProjectConfiguration.UE4RootPath;
			}))
			.Select(s => s?.Also(_ => _.Platform ??= UnrealCookSettings.DefaultPlatformName))
			.ToArray();

		if (dump)
		{
			Console.WriteLine(CookSettings.SerializeObject(Formatting.Indented, JsonIndentation.ReadFromSettings(Directory.GetCurrentDirectory())));
		}
		else
		{
			await Program.CookProject(UnrealItem.RootPath, CookSettings, str => {
				UnrealLogPrinter.WriteLine(str);
			});
		}
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

	[Command("diff", "Launch UE4 diff tool to diff two files.")]
	public async Task DiffAsset([Option(0, "Left file")] string LeftFile, [Option(1, "Right file")] string RightFile
		, [Option("plastic-source", "Optional @sourcesymbolic parameter from Plastic SCM diff to help find project folder when making diff.")] string PlasticSource = null
		, [Option("plastic-destination", "Optional @destinationsymbolic parameter from Plastic SCM diff to help find project folder when making diff.")] string PlasticDestination = null)
	{
		var UnrealItem = UnrealItemDescription.DetectUnrealItemExceptTemp(Directory.GetCurrentDirectory(), UnrealItemType.Project)
			?? UnrealItemDescription.DetectUnrealItemExceptTemp(Path.GetDirectoryName(LeftFile), UnrealItemType.Project)
			?? UnrealItemDescription.DetectUnrealItemExceptTemp(Path.GetDirectoryName(RightFile), UnrealItemType.Project)
			?? UnrealItemDescription.DetectUnrealItemExceptTemp(Path.GetDirectoryName(PlasticSource.GetFilenameFromPlasticRev()), UnrealItemType.Project)
			?? UnrealItemDescription.DetectUnrealItemExceptTemp(Path.GetDirectoryName(PlasticDestination.GetFilenameFromPlasticRev()), UnrealItemType.Project);

		if (UnrealItem == null)
		{
			throw new RequireUnrealItemException(Directory.GetCurrentDirectory(), UnrealItemType.Project);
		}

		var UnrealInstance = new UnrealEngineInstance(UnrealItem);

		Utilities.RequireExecuteCommandLine(ProcessEx.EscapeArguments(
			UnrealInstance.UnrealEditorPath, UnrealItem.FullPath, "-diff", LeftFile, RightFile));
	}

	[Command("merge", "Launch UE4 diff tool to merge conflict file.")]
	public async Task MergeAsset([Option(0, "Base file")] string BaseFile, [Option(1, "Local file")] string LocalFile, [Option(2, "Remote file")] string RemoteFile, [Option(3, "Result file")] string ResultFile)
	{
		var UnrealItem = UnrealItemDescription.DetectUnrealItemExceptTemp(Directory.GetCurrentDirectory(), UnrealItemType.Project)
			?? UnrealItemDescription.DetectUnrealItemExceptTemp(Path.GetDirectoryName(BaseFile), UnrealItemType.Project)
			?? UnrealItemDescription.DetectUnrealItemExceptTemp(Path.GetDirectoryName(LocalFile), UnrealItemType.Project)
			?? UnrealItemDescription.DetectUnrealItemExceptTemp(Path.GetDirectoryName(RemoteFile), UnrealItemType.Project);

		if (UnrealItem == null)
		{
			throw new RequireUnrealItemException(Directory.GetCurrentDirectory(), UnrealItemType.Project);
		}

		var UnrealInstance = new UnrealEngineInstance(UnrealItem);

		if (!File.Exists(ResultFile))
			File.Copy(LocalFile, ResultFile);

		Utilities.RequireExecuteCommandLine(ProcessEx.EscapeArguments(
			UnrealInstance.UnrealEditorPath, UnrealItem.FullPath, "-diff", RemoteFile, LocalFile, BaseFile, ResultFile));
	}

	[Command("merge_lfs", "Launch UE4 diff tool to merge conflict file.")]
	public async Task MergeAssetFromLFS([Option(0, "asset path")] string AssetPath)
	{
		var UnrealItem = UnrealItemDescription.RequireUnrealItem(Directory.GetCurrentDirectory(), UnrealItemType.Project);
		var UnrealInstance = new UnrealEngineInstance(UnrealItem);

		string savedDiffPath = Path.Combine(UnrealItem.RootPath, "Saved\\Diff");
		if (!Directory.Exists(savedDiffPath))
			Directory.CreateDirectory(savedDiffPath);

		string file = AssetPath;
		string fileName = Path.GetFileName(file).Replace('.', '_');
		string baseFile = Path.Combine(savedDiffPath, string.Format("{0}.Base.uasset", fileName));
		string localFile = Path.Combine(savedDiffPath, string.Format("{0}.Local.uasset", fileName));
		string remoteFile = Path.Combine(savedDiffPath, string.Format("{0}.Remote.uasset", fileName));
		string resultFile = Path.Combine(savedDiffPath, string.Format("{0}.Result.uasset", fileName));

		Utilities.RequireExecuteCommandLine(string.Format("git show :1:./{0} | git lfs smudge > {1}", file, baseFile));
		Utilities.RequireExecuteCommandLine(string.Format("git show :2:./{0} | git lfs smudge > {1}", file, localFile));
		Utilities.RequireExecuteCommandLine(string.Format("git show :3:./{0} | git lfs smudge > {1}", file, remoteFile));
		Utilities.RequireExecuteCommandLine(string.Format("git show :1:./{0} | git lfs smudge > {1}", file, resultFile));

		Utilities.RequireExecuteCommandLine(ProcessEx.EscapeArguments(
			UnrealInstance.UnrealEditorPath, UnrealItem.FullPath, "-diff", remoteFile, localFile, baseFile, resultFile));

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

#if false
	//[Command("move", "Move class to another path. NOT IMPLEMENTED.")]
	public async Task MoveClass([Option(0)] string OriginalFileName, [Option(1)] string DestinationPath)
	{
		var UnrealItem = UnrealItemDescription.DetectUnrealItem(Directory.GetCurrentDirectory(), UnrealItemType.Module);

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
#endif

	[Command("create_config", "Create new or update old template config in current project or module.")]
	public async Task CreateConfig()
	{
		var UnrealItem = UnrealItemDescription.RequireUnrealItem(Directory.GetCurrentDirectory(), UnrealItemType.Project, UnrealItemType.Module);

		if (UnrealItem.Type == UnrealItemType.Project)
			Configuration.WriteConfiguration(UnrealItem.Ue4AssistantConfigurationPath, UnrealItem.ReadConfiguration<ProjectConfiguration>() ?? new ProjectConfiguration());
		else if (UnrealItem.Type == UnrealItemType.Module)
			Configuration.WriteConfiguration(UnrealItem.Ue4AssistantConfigurationPath, UnrealItem.ReadConfiguration<TemplateConfiguration>() ?? new TemplateConfiguration());
	}

	void ProtectFolder(string path)
	{
		foreach (string file in Directory.GetFiles(path, "*.*", SearchOption.AllDirectories))
		{
			if (file.EndsWith(".cpp") || file.EndsWith(".h") || file.EndsWith(".cs"))
			{
				var currentAttributes = File.GetAttributes(file);
				File.SetAttributes(file, currentAttributes | FileAttributes.ReadOnly);
			}
		}
	}

	[Command("protect", "Protect Unreal Engine source folder - mark most source files as readonly to avoid accidental change.")]
	public async Task Protect()
	{
		var UnrealItem = UnrealItemDescription.RequireUnrealItem(Directory.GetCurrentDirectory(), UnrealItemType.Engine);

		ProtectFolder(Path.Combine(UnrealItem.RootPath, "Engine", "Source", "Developer"));
		ProtectFolder(Path.Combine(UnrealItem.RootPath, "Engine", "Source", "Editor"));
		ProtectFolder(Path.Combine(UnrealItem.RootPath, "Engine", "Source", "Runtime"));
	}
}
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
