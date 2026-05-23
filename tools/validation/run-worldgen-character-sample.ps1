Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = Resolve-Path (Join-Path $PSScriptRoot '..\..')
dotnet run --project (Join-Path $root 'tools\validation\sample\WorldgenCharacterSample.csproj') --configuration Release
