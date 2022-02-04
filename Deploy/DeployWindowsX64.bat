@ECHO OFF

cd ..

dotnet build AssimpNet/AssimpNet/AssimpNet.csproj -c Release --self-contained -r win-x64 /p:Platform=AnyCPU
dotnet build AssimpNet/AssimpNet.Interop.Generator/AssimpNet.Interop.Generator.csproj -c Release --self-contained -r win-x64 /p:Platform=AnyCPU

dotnet publish CBRE.Editor/CBRE.Editor.csproj -c Release --self-contained -r win-x64 -o Deploy/win-x64 /p:Platform=x64

PAUSE
