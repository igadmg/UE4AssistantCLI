## 1.4.29

1. `add plugin` now not only create plugin but also add it to uproject. If plugin already exist just add it to uproject.

## 1.4.28

1. `build` command can be run in engine folder and will build developmnet editor.
1. `uuid show`\`uuid set` now workd correctly in engine folder.
1. `uuid set` will generate new guid if no parameter set
1. create `.ue.needsetup` to force run of Unreal Engine Setup.bat script one time.

## 1.4.27

1. Set Platform to default value (current platform) if it is not set in `build`/`cook` configuration.

## 1.4.26

1. Added automatic run of `Setup.bat`/`Setup.sh` if needed (needs `.ue.basecommit` in engine source folder)
