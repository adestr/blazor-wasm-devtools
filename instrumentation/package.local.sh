# Remove all local Blazor WASM DevTools NuGet packages for the current user
rm -r /mnt/c/Users/adest/.nuget/packages/blazorwasmdevtools/*

../package.sh

cd ./BlazorWasmDevTools/bin/Release
cp BlazorWasmDevTools*.nupkg /mnt/f/packages/