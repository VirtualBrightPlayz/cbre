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
- Screenshot mode
- Discord RPC

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
