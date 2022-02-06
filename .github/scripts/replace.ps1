param($path,$placeholder,$value)

if(!(Test-Path $path)){
    exit;
}

$name = "PLACEHOLDER"
$enable = "!#USE_$name"
Write-Host "Use $name"

$childItems = Get-ChildItem -Recurse -Path $path
$length = $childItems.Length
Write-Host "Found $length files"
$selection = $childItems | Select-String $enable -List
$length = $selection.Length
Write-Host "Select $length files"
$paths = $selection | Select-Object Path

Write-Host "Execute"
$paths | ForEach-Object {    
    $p = $_.Path;
    Write-Host "Replace {{$placeholder}} with $value in $p"
    (Get-Content $_.Path) -replace "{{$placeholder}}", $value | Set-Content $_.Path
}