<#
.SYNOPSIS
    Устанавливает шаблон «Avalonia MVVM App» в dotnet new и Visual Studio.

.DESCRIPTION
    Шаблон выполнен в формате dotnet new. После установки он доступен
    и в командной строке (dotnet new avalonia-mvvm-app), и в окне
    «Создать проект» Visual Studio, и в Rider.

    Повторный запуск переустанавливает шаблон поверх прежней версии —
    так его удобно обновлять после правок.

.PARAMETER Uninstall
    Удалить ранее установленный шаблон.

.EXAMPLE
    .\Install-Template.ps1

.EXAMPLE
    .\Install-Template.ps1 -Uninstall
#>
[CmdletBinding()]
param(
    [switch] $Uninstall
)

$ErrorActionPreference = 'Stop'

$templateRoot = Split-Path -Parent $PSScriptRoot
$contentDirectory = Join-Path $templateRoot 'content'

if (-not (Test-Path (Join-Path $contentDirectory '.template.config\template.json'))) {
    throw "Не найден template.json в $contentDirectory"
}

if ($Uninstall) {
    Write-Host "Удаление шаблона…"
    dotnet new uninstall $contentDirectory
    exit $LASTEXITCODE
}

# dotnet new install поверх уже установленного шаблона сообщает об ошибке,
# поэтому прежнюю версию снимаем заранее и молча.
# Тихое снятие: dotnet new uninstall пишет в stderr, если шаблон не установлен,
# а PowerShell 5.1 превращает вывод native-команды в stderr в ошибку.
$null = & cmd /c "dotnet new uninstall ""$contentDirectory"" >nul 2>&1"

Write-Host "Установка шаблона из $contentDirectory …"
dotnet new install $contentDirectory

if ($LASTEXITCODE -ne 0) {
    throw "Не удалось установить шаблон (код $LASTEXITCODE)."
}

Write-Host ""
Write-Host "Готово." -ForegroundColor Green
Write-Host ""
Write-Host "Командная строка:"
Write-Host "    dotnet new avalonia-mvvm-app -n MyApp"
Write-Host "    dotnet new avalonia-mvvm-app -n MyApp --Android true --iOS true"
Write-Host ""
Write-Host "Visual Studio: перезапустите среду, затем «Создать проект» -> поиск «Avalonia MVVM App»."
