Write-Host "Iniciando compilacion de PoPUnturned Suite..." -ForegroundColor Cyan

# Directorios de trabajo
$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrEmpty($scriptRoot)) {
    $scriptRoot = Get-Location
}

$publishDir = Join-Path $scriptRoot "src/publish"
if (Test-Path $publishDir) {
    Remove-Item -Recurse -Force $publishDir
}
New-Item -ItemType Directory -Force -Path (Join-Path $publishDir "launcher")
New-Item -ItemType Directory -Force -Path (Join-Path $publishDir "installer")

# 1. Compilar Launcher en modo Release
Write-Host "Compilando PoPUnturnedLauncher..." -ForegroundColor Yellow
dotnet publish src/PoPUnturnedLauncher/PoPUnturnedLauncher.csproj -c Release -f net8.0-windows -o "src/publish/launcher"

if ($LASTEXITCODE -ne 0) {
    Write-Error "Error al compilar PoPUnturnedLauncher."
    exit $LASTEXITCODE
}

# Limpiar archivos .pdb innecesarios para reducir el peso
Get-ChildItem "src/publish/launcher" -Filter *.pdb | Remove-Item -Force

# 2. Empaquetar el Launcher en un Zip
Write-Host "Empaquetando Launcher en LauncherFiles.zip..." -ForegroundColor Yellow
$zipPath = Join-Path $scriptRoot "src/PoPUnturnedInstaller/LauncherFiles.zip"
if (Test-Path $zipPath) {
    Remove-Item -Force $zipPath
}
Compress-Archive -Path "src/publish/launcher/*" -DestinationPath $zipPath -Force

# 3. Compilar el Instalador en modo Release (el cual incrustará el LauncherFiles.zip generado)
Write-Host "Compilando PoPUnturnedInstaller..." -ForegroundColor Yellow
dotnet publish src/PoPUnturnedInstaller/PoPUnturnedInstaller.csproj -c Release -f net8.0-windows -r win-x64 -p:PublishSingleFile=true -p:PublishReadyToRun=true -p:SelfContained=false -o "src/publish/installer"

if ($LASTEXITCODE -ne 0) {
    Write-Error "Error al compilar PoPUnturnedInstaller."
    exit $LASTEXITCODE
}

# Limpiar PDBs del instalador
Get-ChildItem "src/publish/installer" -Filter *.pdb | Remove-Item -Force

# 4. Copiar el instalador compilado a la raíz con un nombre amigable
Copy-Item "src/publish/installer/PoPUnturnedInstaller.exe" "./PoPUnturnedSetup.exe" -Force

$setupPath = (Resolve-Path "./PoPUnturnedSetup.exe").Path

Write-Host ""
Write-Host "=============================================" -ForegroundColor Green
Write-Host "COMPILACION Y EMPAQUETADO COMPLETADOS CON EXITO!" -ForegroundColor Green
Write-Host "El instalador automatico se encuentra en:" -ForegroundColor Green
Write-Host " -> $setupPath" -ForegroundColor White
Write-Host "=============================================" -ForegroundColor Green
