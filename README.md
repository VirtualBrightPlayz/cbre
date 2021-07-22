Containment Breach Room Editor
======

omg it's not 3dws so it sux :(

This is a hacked-up fork of Sledge meant for room creation for SCP - Containment Breach.

The Editor is licensed under the GNU General Public License, version 2.0.
All other components are licensed under the GNU Lesser General Public License, version 2.1, unless otherwise stated.

You can find the original source code at https://github.com/LogicAndTrick/sledge

# Building

## Required SDKs

.NET 5

### Assimp

In the folder `AssimpNet/AssimpNet.Interop.Generator/` run the command `dotnet build`

### CBRE.Editor

In the folder `CBRE.Editor/` run the command `dotnet build`

## Running

### Windows 64-bit

Run the file `CBRE.Editor/bin/Debug/net5.0/CBRE.Editor.exe`

### Linux 64-bit (Not tested yet)

Run the file `CBRE.Editor/bin/Debug/net5.0/CBRE.Editor.dll` using the `dotnet` command

## Contributing

Just make a [pull request](https://github.com/juanjp600/cbre/pulls). Try to keep the style consistent with existing code.

