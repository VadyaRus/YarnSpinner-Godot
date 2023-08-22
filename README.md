# YarnSpinner-Godot

This is meant to be used as a git submodule. The entire contents of this repository will go into a sub-folder of the addons folder. This is a C# solution that is based on a combination of [GDYarn](https://github.com/kyperbelt/GDYarn) and the official [YarnSpinner-Unity](https://github.com/YarnSpinnerTool/YarnSpinner-Unity/tree/v2.2.4).

# Installation

It is suggested to add this library as a git submodule.

To clone the submodule inside of the addons folder run:
```
mkdir addons
git submodule add https://github.com/VadyaRus/YarnSpinner-Godot.git addons/YarnSpinner-Godot
```

This will clone the plugin repo inside your addons folder.

## Configure solution

You'll need to add the Yarn DLLs into your C# solution so that the compiler can path them when building.
If there is no .csproj file in the root of your project, you can generate a new one from the editor. Go to the Project Tab -> Tools -> C# -> Create C# solution

Add the following to your {project-name}.csproj inside the top level `<Project> </Project>` node
```
<ItemGroup>
  <Reference Include="addons\YarnSpinner-Godot\Runtime\DLLs\**" />
</ItemGroup>
```

Note: Make sure to rebuild your project after editing your csproj file

## Enable the plugin

Access the plugin settings for your project from the editor by navigating to the
`Project Tab -> Project Settings`
and locating the `Plugins` tab.

You should see the YarnSpinner-Godot plugin, click the pencil icon to edit the plugin settings and make sure the language is set to C# and not GDScript.

Then click the Enable button to enable the plugin.
