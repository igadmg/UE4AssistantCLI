Command line tools for working with UE4 projects.
Full Windows support. 
Most commands work in OSX (project, plugin, module and code generation) some need improvement (opening project and building from command line is not tested yet)
Build and Cook are working in Linux.

```
Usage: ue4cli <Command>

Commands:
  add module <Name>                             Add new module to current project or plugin.
  add plugin <Name>                             Create and add new plugin to current project.
  add bpfl [Name]                               Add a function library to current module, if one does not exist.
  add class <Name> <BaseType>                   Add new class to current module.
  add dataasset <Name>                          Add a data asset class with given name.
  add interface <Name>                          Add new interface to current module.
  add project <Name>                            [Deprecated] Create new project.
  add tablerow <Name>                           Add a data table row struct with given name.
  build [Config] [--dump]                       Build project. --dump - Dump configuration file to the console.
  clean                                         Clean project and plugins from build files.
  code                                          Open Source Code Editor for current project.
  convert                                       Convert copied clipboard data to json file.
  cook [Config] [--dump]                        Cook project with cook settings. --dump - Dump configuration file to the console.
  create_config                                 Create new or update old template config in current project or module.
  diff <LeftFile> <RightFile>                   Launch UE4 diff tool to diff two files.
  editor                                        Open UE4 Editor for current project.
  fix all                                       Fix all possible errors.
  fix dll_load                                  Fix `Failed to load dll` errors.
  fix pch_cleanup                               Cleanup PCH file errors.
  generate                                      Generate Source Code Solution for current project.
  get_ue_root                                   Get UE root of associated UE build.
  help                                          Display help.
  init                                          [Deprecated] Initialize working environment, create Libraries.sln.
  log build                                     Open build log folder.
  log project                                   Open project log folder.
  merge [Base] [Local] [Remote] [Result]        Launch UE4 diff tool to merge conflict file.
  merge_lfs <File>                              Launch UE4 diff tool to merge conflict file. Works for Git LFS conflict files.
  run <Commandlet> [parameters]                 Run commandlet.
  uuid list                                     List registered Unreal Engine uuid identifiers.
  uuid set <GUID>                               Set project's Unreal Engine uuid identifier.
  uuid show                                     Show project's Unreal Engine uuid identifier.
  version                                       Display ue4cli tool version.
```

## Setup tool

`ue4cli` tool can be easely installed from nuget with `dotnet tool install -g ue4cli` command (will install globally). And updated with `dotnet tool update -g ue4cli` command.

## Source generation commands.

Two types of `add` commands:
1. Plugin/Module generation - create code plugin or module with minimal required files.
    - `add plugin` - generate plugin in `Plugins/name` folder and add it to `.uproject` file. If plugin already exist command will just add it to the project. (works only for local plugins)
    - `add module` - generate module in current folder. Will add module to `.uproject` or to `.uplugin` but not add to `.Build.cs` scripts.
1. Source generation - `add bpfl`, `add class`, `add dataasset`, `add interface`, `add tablerow` - automaticaly add source and header files based on current directory and source folder structure (headers are put to `Public`, source files are put to `Private` if these folders exist).

## Management commands.
Various commands usefull in day to day work. They can be used form any path inside project folder tree.
1. `generate` - regenerate solution and project files.
1. `code` - open code editor (can autogenertate project if set by config, default off)
1. `editor` - open Unreal Engine editor (can autobuild project if set by config, default off)

## Build/Cook commands.
1. `build` and `cook` commands are supplied with json configuration file. json configuration file can be generated with `ue4cli build --dump` command. Json config contains various flags passed to `BuildCookRun` RunAU command. Both commands will check the existance of `.ue.needsetup` in source engine root or compare timestamp of `.ue.basecommit` with `.ue4dependencies` timestamp. In any of these cases `Setup.bat` script would be run (if source is not destributed via git it help to automate engine recompile after version upgrade)
1. `build` command if run in unreal engine folder will build engine from source. It will use UAT to build "Build Tools" fist and then use UBT to build Editor target. As I beleive that is the minimum to launch editor.

### Build/Cook source engine build update checking

When building from source it is requrede to run `Setup.bat` or `Setup.sh` on first build or after Unreal Engine version upgrade. That step is automated in `build`/`cook` commands. Fot this automation to work, create `.ue.basecommit` file in UnrealEngine roou folder (where `Setup.bat` is located). I prefer to store git commit hash in that file. That file should be touched or updated every time Unreal Engine version is upgraded. Then on `build` step it's timestamp is compared to `.ue4dependencies` timestemp, and if it is newer or `.ue4dependencies` does not exist then `Setup.bat` or `Setup.sh` is run automaticaly.

## Config commands.
1. `create_config` - create `.ue4a` config file inside `Project` or `Module` folder. Different configs for each type. Run this command to create local config file.
    - `Project`: 
```
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
    - `Module`: these options are passed directly source code templates.
```
{
  "pchHeader": null,  // should generate PCH include string, and pch name.
  "finalHeader": "MyProject.final.h", // should generate final include header in source files, and it's name.
  "locTextNamespaceName": "MyProject" // should generate LOCTEXT_NAMESPACE define/undef pair and it's value.
}
```

## Blueprint Diff/Merge commands.
Thesee commands run project assosiated Unreal Engine in diff or merge mode. Can be used form source control tools. How and if `Merge` works in unknown. Diff is only usefull to me.
1. `diff` - run diff mode.
1. `merge` - run merge mode. 
Diff command have special flags for use from plastic SCM. Needed to guess project folder correctly.

### Plastic SCM diff/merge command.
1. `diff` - `ue4cli diff "@sourcefile" "@destinationfile" -plastic-source "@sourcesymbolic" -plastic-destination "@destinationsymbolic"`
1. `merge` - `ue4cli merge "@basefile" "@destinationfile" "@sourcefile" "@output"`

## Engine UUID manipulation commands.
When working with source engine build it cna be usefull to set one UUID for engine association across all developer computers. That can be easely done with
`ue4cli uuid set {1842737E-4FB8-4CF3-A097-AD89D06349D8}`
1. `uuid list` - list registered Unreal Engine uuid identifiers.
1. `uuid set <GUID>` - set project's Unreal Engine uuid identifier for current folder. Can be run without GUID parameter to set auto-generated guid. If run in engine folder will register engine instance or set engine guid.
1. `uuid show` - show projects uuid.

## Other commands.

1. `log` command cnab used to open either `project` or `build` (engine) log folder.
1. `fix pch_cleanup` command will scan build logs and find .pch complaints and delete that files (fatal error C1853). Should be run after failed build.
1. `fix dll_load` command will scan run logs and find if some dlls failed to load and delete them.
1. `fix all` apply all fixes.

# Appengix
## Project Build/Cook configuration

```
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

- `StringParameter` - adds -param={value} if field is not null or empty.
- `BoolParameter` - adds -param or -noparam (or none) if enbaled or disabled.
- `StringListParameter` - adds -param={v1}+{v2}+...+{vn} if not null or mepty.
