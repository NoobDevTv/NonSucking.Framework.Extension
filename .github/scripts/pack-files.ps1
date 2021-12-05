param($path)

if(!(Test-Path $path)){
    exit;
}

$name = "Nuget"
$enable = "!#USE_$name"
Write-Host "Use $name"

$childItems = Get-ChildItem -Recurse -Path $path -Include *.csproj
$length = $childItems.Length
Write-Host "Found $length files"
$selection = $childItems | Select-String $enable -List
$length = $selection.Length
Write-Host "Select $length files"
$paths = $selection | Select-Object Path

Write-Host "Execute"
$paths | ForEach-Object {    
    $p = $_.Path;
    Write-Host "Pack Project $p" 
    dotnet pack $_.Path -c Release --no-build 
}