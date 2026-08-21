# Build

## Prerequisites

- Windows for the full solution build and tests.
- .NET 8 SDK.
- .NET 9 SDK.
- Git submodules initialized.

```powershell
git submodule update --init --recursive
```

## Validate

Run these commands from the repository root:

```powershell
dotnet restore UE4AssistantCLI.sln
dotnet build UE4AssistantCLI.sln --configuration Release --no-restore
dotnet test UE4AssistantCLI.sln --configuration Release --no-build --verbosity normal
dotnet pack UE4AssistantCLI/UE4AssistantCLI.csproj --configuration Release --no-build
```

## Install local tool package

After packing, install the CLI from the local package output:

```powershell
dotnet tool install --global uecli --source UE4AssistantCLI/nupkg
```

Update an existing local install with:

```powershell
dotnet tool update --global uecli --source UE4AssistantCLI/nupkg
```
