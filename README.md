Containment Breach Room Editor
======

The Editor is licensed under the GNU General Public License, version 2.0.
All other components are licensed under the GNU Lesser General Public License, version 2.1, unless otherwise stated.

You can find the original source code at https://github.com/SCP-CBN/cbre

# Why?

- 3D World Studio is not free, does not work outside of Windows 7, and is no longer maintained by its creators.
- The official CBRE fork is no longer maintained.
- CBRE still can be useful outside the scope of the official SCP-CB 1.4 update that was cancelled.

# Features

- `.rmesh` file exporting
- Lightmapping
- (WIP) GPU Lightmapping
- Lightmapping with Blender
- Screenshot mode
- Discord RPC

# Download

Make sure to install vc_redist_x86 from [here](https://docs.microsoft.com/en-US/cpp/windows/latest-supported-vc-redist) when on Windows

There is a GitHub Actions script so you can download the latest from [the Actions tab](https://github.com/VirtualBrightPlayz/cbre/actions).

# Blender Lightmapping

Install [Naxela's The Lightmapper addon for Blender](https://github.com/Naxela/The_Lightmapper)

Copy the files from `BlenderBaking` into your install of `CBRE.Editor` and change the `raytrace-lm.bat`'s contents to point to a valid install of Blender.

# Building

## Required SDKs

.NET 6

### nfd

For the time being, compile nfd/NativeFileDialog yourself. I'll consider implementing it better later.

### Assimp

In the folder `AssimpNet/AssimpNet.Interop.Generator/` run the command `dotnet build`

### CBRE.Editor

In the folder `CBRE.Editor/` run the command `dotnet build /p:Platform="x64"`

## Running

### Windows 64-bit

Run the file `CBRE.Editor/bin/Debug/net6.0/CBRE.Editor.exe`

### Linux 64-bit (Not tested yet)

Run the file `CBRE.Editor/bin/Debug/net6.0/CBRE.Editor.dll` using the `dotnet` command

## Contributing

Just make a [pull request](https://github.com/VirtualBrightPlayz/cbre/pulls). Try to keep the style consistent with existing code.
