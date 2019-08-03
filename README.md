Command line tools for working with UE4 projects.
Full Windows support. Most commands work in OSX (project, plugin, module and code generation) some need improvement (opening project and building from command line is not tested yet)

```
ua-cli <command> <parameters>
    add <item> <params> - add something to the project
available items to add:
        project <ProjectName> - create new project
        plugin <PluginName> - create and add new plugin to current project.
        module <ModuleName> - add new module to current project or plugin.
        interface <InterfaceName> - add new interface to current module.
        class <ClassName> <BaseType> - add new class to current module.
        bpfl - add a function library to current module, if one does not exist.
        dataasset <ClassName> <BaseType> - add a data asset class with given name. BaseType is optional.

    init <vs version> - initialize working environment, create Libraries.sln.
    clean - clean project and plugins from build files.
    code - open code editor for project.
    editor - open UE4 editor for project.
    generate - generate VS project for project.
    get_ue_root <ProjectName> - get UE root of associated UE build.
    build - build project.
    cook <CookSettings> - cook project with cook settings.

    convert - copied clipboard data to json file.

    merge - launch UE4 diff tool to merge conflict file.
```
