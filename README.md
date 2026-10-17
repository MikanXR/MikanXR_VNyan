# MikanXR VNyan Plugin

A [VNyan](https://vnyan.net) plugin that mirrors a running [MikanXR](https://github.com/MikanXR/MikanXR) editor into VNyan's scene and renders the avatar back into the editor's composite. The editor's stages, anchors, stencils and cameras appear under a Mikan root in VNyan; each Mikan camera renders the VNyan scene with the editor's calibrated lens, cuts the avatar out behind the editor's stencils, catches its shadow on them, and publishes color, depth and shadow buffers to the editor through Spout.

The plugin is one managed assembly plus a shader bundle. It compiles the [MikanXR Unity package](https://github.com/MikanXR/MikanXR_Unity) against VNyan's own Unity, VNyanInterface, KlakSpout and Newtonsoft.Json assemblies, so it loads no native library of its own.

## Layout

- `MikanXR_VNyan.csproj`: the plugin assembly. It compiles `Runtime/**/*.cs` of the Unity package by link plus `Source/`.
- `Source/`: the VNyan side. The plugin manifest VNyan starts, the settings store over VNyan's own, and the plugin window.
- `MikanXR_VNyanSDK/`: a Unity 2022.3.62f3 project with the VNyan SDK and the package, for VNyan-side Unity work such as custom objects.
- `install.bat`: copies the assembly and the shader bundle into a VNyan install.

## Building

Requirements: the .NET SDK, VNyan 2022.3.62f3 or later installed, and a checkout of `MikanXR_Unity` beside this one (`..\MikanXR_Unity`), with its bindings fetched from a MikanXR client SDK.

```bat
dotnet build MikanXR_VNyan.csproj -c Release
```

`VNyanDir` defaults to `D:\tools\vnyan`; pass `-p:VNyanDir=<path>` or set `VNYAN_DIR` for another install. `MikanUnityPackageDir` points the build at another package checkout.

The shader bundle comes from the Unity package: open `MikanXR_Unity` in Unity and run `MikanXR / Build Shader Bundle`, or in batch mode:

```bat
"C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe" -batchmode -quit -projectPath ..\MikanXR_Unity -executeMethod MikanXR.Unity.Editor.MikanShaderBundleBuild.BuildShaderBundle -outputDir %CD%\Build
```

## Installing

```bat
install.bat D:\tools\vnyan
```

puts `MikanXR_VNyan.dll` in `Items\Assemblies` and `mikanxr_shaders.bundle` in `Items\Assemblies\MikanXR`. Turn on "Allow 3rd party Mods/Plugins" in VNyan's Misc settings and restart VNyan. A MikanXR button appears in the Plugins window.

## Using

The window shows the connection, the editor host and port, the world scale (Unity units per Mikan meter, 10 by default since VNyan avatars are human sized and the editor's scenes are tabletop sized), and the render settings: whether stencils render as knockouts, whether shadows are caught on them, whether the depth buffer is published (off by default, since the knockout already handles occlusion), whether camera frustums are drawn, whether the stencils show in VNyan's own view as translucent previews and the spot light cones as outlines, whether the editor's light environment and DMX spot lights light the VNyan scene and at what intensity scale, and the shadow strength. Below them, the Mikan root's placement in the VNyan scene and, per stage, the stage's placement. Editing a stage's fields pins it: the editor's own transform for that stage is ignored until "Follow editor". Everything persists in `MikanXRPlugin.cfg` under VNyan's profile.

In the editor, the compositor that should carry VNyan's avatar uses `unity_client_shadow_graph.compgraph` with a client texture source named `VNyan`, the plugin's client id. The Mikan cameras render every layer but VNyan's UI, and the plugin's own helpers hide themselves from them, so the avatar and VNyan's world both appear.

## Status

Builds against VNyan 2022.3.62f3 and the Unity package's test suite covers the shared runtime. A runtime transform gizmo for placing the root and the stages by dragging, carried over from the previous plugin, is not part of this version; the window's fields cover placement.
