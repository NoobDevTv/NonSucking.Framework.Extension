param($file, $branch)

$version = "<Version>1.0.0-alpha.{NUM}-{HASH:SHORT}</Version>"
$assemblyVersion = "<AssemblyVersion>0.0.0.{NUM}</AssemblyVersion>"
$fileVersion = "<FileVersion>0.0.0.{NUM}</FileVersion>"

Write-Host "setup version on" $file
Write-Host "-----------------------------"

git fetch
Write-Host "-----------------------------"

Write-Host "get commit count"
$num = git rev-list --count $branch
Write-Host $num
Write-Host "-----------------------------"

Write-Host "get current short hash"
$shortHash = git rev-parse --short HEAD
Write-Host $shortHash
Write-Host "-----------------------------"

Write-Host "get file content"
$fileContent = Get-Content -Path $file
Write-Host "-----------------------------"

Write-Host "Replace {VERSION}"
$fileContent = $fileContent -replace "<!--{VERSION}-->", $version
Write-Host "-----------------------------"

Write-Host "Replace {ASSEMBLY:VERSION}"
$fileContent = $fileContent -replace "<!--{ASSEMBLY:VERSION}-->", $assemblyVersion
Write-Host "-----------------------------"

Write-Host "Replace {FILE:VERSION}"
$fileContent = $fileContent -replace "<!--{FILE:VERSION}-->", $fileVersion
Write-Host "-----------------------------"

Write-Host "Replace {NUM}"
$fileContent = $fileContent -replace "{NUM}", $num
Write-Host "-----------------------------"

Write-Host "Replace {HASH:SHORT}"
$fileContent = $fileContent -replace "{HASH:SHORT}", $shortHash
Write-Host "-----------------------------"

Write-Host "Write file"
Set-Content -Path $file -Value $fileContent