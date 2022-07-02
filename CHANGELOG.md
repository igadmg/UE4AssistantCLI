## 1.5.2

1. Fixed ConsoleColors output to ANSI console. Also fixed MacOS color output. 

## 1.5.0

1. Added simple color output for `build` and `cook` commands.

## 1.4.32

1. Finally fixed relative path handling for engine and packaged builds. Now all path are relative to corresponding unreal project ite (module, project or engine root).

## 1.4.31

1. Added `run` command to run commandlets. Can be used like `ue4cli run CompileAllBlueprints` also can be followe by commandlet parameters.

## 1.4.30

1. Moved `pch_cleanup` under `fix` subcommand.
1. Added `fix dll_load` command will scan run logs and find if some dlls failed to load and delete them so they can be recompiled.

## 1.4.29

1. `add plugin` now not only create plugin but also add it to uproject. If plugin already exist just add it to uproject.
1. `editor` command in engine source folder will open Unreal Engine editor without project.
1. `build` command will also build "Build Tools" for platform before building an editor.

## 1.4.28

1. `build` command can be run in engine folder and will build developmnet editor.
1. `uuid show`\`uuid set` now workd correctly in engine folder.
1. `uuid set` will generate new guid if no parameter set
1. create `.ue.needsetup` to force run of Unreal Engine Setup.bat script one time.

## 1.4.27

1. Set Platform to default value (current platform) if it is not set in `build`/`cook` configuration.

## 1.4.26

1. Added automatic run of `Setup.bat`/`Setup.sh` if needed (needs `.ue.basecommit` in engine source folder)
