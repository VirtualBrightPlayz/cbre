@ECHO OFF

cd AssimpNet
dotnet build assimp-net.sln -c Release --self-contained -r win-x86 /p:Platform="Any CPU"

cd .. 
dotnet publish CBRE.sln -c Release --self-contained -r win-x86 /p:Platform="Any CPU"

PAUSE
