param($file, $branch)

Write-Host "setup version on" + $file

Write-Host "get commit count"
$num = git rev-list --count $branch

Write-Host "get current short hash"
$shortHash = git rev-parse --short HEAD

Write-Host "get file content"
$fileContent = Get-Content -Path $file

Write-Host "Replace {NUM}"
$fileContent = $fileContent -replace "{NUM}", $num

Write-Host "Replace {HASH:SHORT}"
$fileContent = $fileContent -replace "{HASH:SHORT}", $shortHash

Write-Host "Write file"
Set-Content -Path $file -Value $fileContent