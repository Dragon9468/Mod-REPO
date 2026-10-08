$gameDir = "D:\New folder\steam\steamapps\common\REPO"
$distDir = Join-Path $PSScriptRoot "dist\REPO_Portable_Mod"

if (-not (Test-Path $gameDir)) {
    Write-Host "Game directory not found: $gameDir" -ForegroundColor Red
    exit 1
}

Write-Host "Installing mod to: $gameDir ..." -ForegroundColor Cyan
Copy-Item -Path "$distDir\*" -Destination $gameDir -Recurse -Force
Write-Host "Done! Mod installed successfully. Press Insert or F1 in-game to open menu." -ForegroundColor Green
