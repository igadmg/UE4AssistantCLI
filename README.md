Command line tools for working with Unreal Engine projects.

- Full Windows support.
- Most project, plugin, module, and code-generation commands work on macOS, but opening projects and command-line builds need more validation.
- Build and cook commands work on Linux.

```
Usage: uecli <Command>

Commands:
  add module <Name>                             Add new module to current project or plugin.
  add plugin <Name>                             Create and add new plugin to current project.
  add bpfl [Name]                               Add a function library to current module, if one does not exist.
  add class <Name> <BaseType>                   Add new class to current module.
  add dataasset <Name>                          Add a data asset class with given name.
  add interface <Name>                          Add new interface to current module.
  add project <Name>                            [Deprecated] Create new project.
  add tablerow <Name>                           Add a data table row struct with given name.
  build [Config] [--dump]                       Build project. --dump dumps configuration to the console.
  clean                                         Clean project and plugins from build files.
  code                                          Open source code editor for current project.
  config get [File] [Section] [Key]             Get value from config file.
  config set [File] [Section] [Key] <Value>     Set value in config file. Value can be piped through stdin.
  convert                                       Convert copied clipboard data to JSON.
  cook [Config] [--dump]                        Cook project with cook settings. --dump dumps configuration to the console.
  create_config                                 Create new or update old template config in current project or module.
  diff <LeftFile> <RightFile>                   Launch Unreal diff tool to diff two files.
  editor                                        Open Unreal Editor for current project.
  fix all                                       Fix all possible known errors.
  fix dll_load                                  Fix `Failed to load dll` errors.
  fix pch_cleanup                               Clean up PCH file errors.
  generate                                      Generate source code solution for current project.
  get_ue_root                                   Get UE root of associated Unreal Engine build.
  help                                          Display help.
  init                                          [Deprecated] Initialize working environment, create Libraries.sln.
  log build                                     Open build log folder.
  log project                                   Open project log folder.
  merge [Base] [Local] [Remote] [Result]        Launch Unreal diff tool to merge conflict file.
  merge_lfs <File>                              Launch Unreal diff tool to merge Git LFS conflict file.
  protect                                       Mark Unreal Engine source files as read-only.
  run <Commandlet> [parameters]                 Run commandlet.
  uuid list                                     List registered Unreal Engine UUID identifiers.
  uuid set <GUID>                               Set project's Unreal Engine UUID identifier.
  uuid show                                     Show project's Unreal Engine UUID identifier.
  version                                       Display uecli tool version.
```

## Setup tool

Install the tool from NuGet:

```powershell
dotnet tool install --global uecli
```

Update it with:

```powershell
dotnet tool update --global uecli
```

## Building from source

See [BUILD.md](BUILD.md) for prerequisites, submodule setup, restore, build, test, and pack commands.

## Source generation commands

There are two types of `add` commands:

1. Plugin/module generation creates code plugins or modules with minimal required files.
   - `add plugin` generates a plugin in the `Plugins/<Name>` folder and adds it to the `.uproject` file. If the plugin already exists, the command only adds it to the project. This works only for local plugins.
   - `add module` generates a module in the current folder. It adds the module to `.uproject` or `.uplugin`, but does not add it to `.Build.cs` scripts.
2. Source generation commands (`add bpfl`, `add class`, `add dataasset`, `add interface`, `add tablerow`) automatically add source and header files based on the current directory and source folder structure. Headers are placed in `Public`, and source files are placed in `Private` when those folders exist.

## Management commands

Useful day-to-day commands can be run from any path inside a project folder tree.

1. `generate` regenerates solution and project files.
2. `code` opens the code editor and can auto-generate the project if configured. This is off by default.
3. `editor` opens Unreal Editor and can auto-build the project if configured. This is off by default.

## Build/Cook commands

`build` and `cook` commands use JSON configuration files. A build configuration can be generated with:

```powershell
uecli build --dump
```

Both commands check for `.ue.needsetup` in a source engine root or compare the timestamp of `.ue.basecommit` with `.ue4dependencies`. If setup is required, `Setup.bat` or `Setup.sh` is run automatically. This helps automate engine recompilation after version upgrades.

If `build` is run in an Unreal Engine source folder, it builds the engine from source. It uses UAT to build "Build Tools" first, then uses UBT to build the editor target.

### Build/Cook source engine update checking

When building from source, `Setup.bat` or `Setup.sh` must be run on the first build or after an Unreal Engine version upgrade. To automate this step, create a `.ue.basecommit` file in the Unreal Engine root folder where `Setup.bat` is located. Store the engine commit hash or another marker in that file and update its timestamp whenever the engine version changes. During build, its timestamp is compared to `.ue4dependencies`; if it is newer or `.ue4dependencies` does not exist, setup is run automatically.

## Config commands

`create_config` creates a `.ue4a` config file inside a project or module folder.

Project config:

```json
{
	"UE4RootPath": "<path to UE Editor (for source builds) null for store builds.>",
	"GenerateProject": {   // should run generate command on these commands
		"onAddItem": true, // add *
		"onCode": true,    // code
		"onEditor": false, // editor
		"onBuild": false,  // build
		"onCook": false    // cook
	},
	"InterfaceSuffix": "Interface",       // suffix added to `add interface` type names
	"FunctionLibrarySuffix": "Statics",   // suffix added to `add bpfl` type names
	"DefaultBuildConfigurationFile": "Build.json",   // default configuration for `build` command
	"DefaultCookConfigurationFile": "Cook.json",     // default configuration for `cook` command
	"JsonIndentation": {   // json formatting options for generated files.
		"IndentationChar": "\t",
		"IndentationLevel": 1
	}
}
```

Module config options are passed directly to source code templates:

```json
{
  "pchHeader": null,  // should generate PCH include string, and pch name.
  "finalHeader": "MyProject.final.h", // should generate final include header in source files, and it's name.
  "locTextNamespaceName": "MyProject" // should generate LOCTEXT_NAMESPACE define/undef pair and it's value.
}
```

## Blueprint Diff/Merge commands

These commands run the project-associated Unreal Engine in diff or merge mode. They can be used from source control tools. Diff is the most commonly used workflow; merge support depends on Unreal Engine behavior.

1. `diff` runs diff mode.
2. `merge` runs merge mode.

### Plastic SCM diff/merge commands

```powershell
uecli diff "@sourcefile" "@destinationfile" -plastic-source "@sourcesymbolic" -plastic-destination "@destinationsymbolic"
uecli merge "@basefile" "@destinationfile" "@sourcefile" "@output"
```

## Engine UUID manipulation commands

When working with source engine builds, it can be useful to set one UUID for engine association across all developer computers:

```powershell
uecli uuid set {1842737E-4FB8-4CF3-A097-AD89D06349D8}
```

1. `uuid list` lists registered Unreal Engine UUID identifiers.
2. `uuid set <GUID>` sets the project's Unreal Engine UUID identifier for the current folder. It can be run without a GUID to use an auto-generated GUID. If run in an engine folder, it registers the engine instance or sets the engine GUID.
3. `uuid show` shows the project's UUID.

## Other commands

1. `log` opens either the project log folder or build log folder.
2. `fix pch_cleanup` scans build logs for `.pch` complaints and deletes those files after fatal error C1853.
3. `fix dll_load` scans run logs for failed DLL loads and deletes those DLLs.
4. `fix all` applies all fixes.

## Project Build/Cook configuration

```csharp
[StringParameter("project")] public string ProjectFullPath = null;
[StringParameter("target")] public string Target = null;
[StringParameter("platform")] public string Platform = DefaultPlatformName;
[StringParameter("cookflavor")] public string CookFlavor = null;
[StringParameter("clientconfig")] public string ClientConfig;
[StringParameter("serverconfig")] public string ServerConfig;
[StringParameter("ddc")] public string DDC;
[BoolParameter("skipbuildeditor")] public bool? SkipBuildEditor = null;
[BoolParameter("P4", "noP4")] public bool? UseP4 = null;
[BoolParameter("cook")] public bool? Cook = null;
[BoolParameter("allmaps")] public bool? AllMaps = null;
[BoolParameter("client", "noclient")] public bool? Client = null;
[BoolParameter("server", "noserver")] public bool? Server = null;
[BoolParameter("build")] public bool? Build = null;
[BoolParameter("stage")] public bool? Stage = null;
[BoolParameter("pak")] public bool? Pak = null;
[BoolParameter("archive")] public bool? Archive = null;
[BoolParameter("package")] public bool? Package = null;
[BoolParameter("compressed")] public bool? Compressed = null;
[BoolParameter("NoXGE")] public bool? NoXGE = null;
[BoolParameter("NoFASTBuild")] public bool? NoFASTBuild = null;
[BoolParameter("utf8output")] public bool? Utf8Output = null;
[BoolParameter("unversionedcookedcontent")] public bool? UnversionedCookedContent = null;
[BoolParameter("generatepatch")] public bool? GeneratePatch = null;
[StringParameter("createreleaseversion")] public string CreateReleaseVersion;
[StringParameter("basedonreleaseversion")] public string BasedOnReleaseVersion;
[StringParameter("archivedirectory")] public string ArchiveDirectory;
[StringListParameter("map")] public string[] Maps;
[StringListParameter("CookCultures")] public string[] CookCultures;
```

- `StringParameter` adds `-param={value}` if the field is not null or empty.
- `BoolParameter` adds `-param`, `-noparam`, or nothing if enabled, disabled, or unset.
- `StringListParameter` adds `-param={v1}+{v2}+...+{vn}` if not null or empty.
