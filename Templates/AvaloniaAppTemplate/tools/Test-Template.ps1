<#
.SYNOPSIS
    Проверяет шаблон: создаёт по нему решение и собирает его.

.DESCRIPTION
    Шаблон считается рабочим, только если созданное по нему решение
    собирается. Сценарий устанавливает шаблон, создаёт решение во временном
    каталоге и выполняет dotnet build.

    По умолчанию проверяется настольная конфигурация: сборка мобильных голов
    требует установленных рабочих нагрузок (android, ios, wasm-tools),
    которых может не быть на машине разработчика.

.PARAMETER AllPlatforms
    Включить в проверяемое решение Android, iOS и браузер. Требует
    соответствующих рабочих нагрузок.

.PARAMETER Name
    Имя проверочного решения.

.PARAMETER KeepOutput
    Не удалять созданное решение после проверки.

.EXAMPLE
    .\Test-Template.ps1

.EXAMPLE
    .\Test-Template.ps1 -AllPlatforms -KeepOutput
#>
[CmdletBinding()]
param(
    [switch] $AllPlatforms,
    [string] $Name = 'TemplateSmokeTest',
    [switch] $KeepOutput
)

$ErrorActionPreference = 'Stop'

$templateRoot = Split-Path -Parent $PSScriptRoot
$contentDirectory = Join-Path $templateRoot 'content'
$outputRoot = Join-Path $env:TEMP 'avalonia-template-test'

if (Test-Path $outputRoot) {
    Remove-Item $outputRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null

Write-Host "Установка шаблона…"
# Тихое снятие: dotnet new uninstall пишет в stderr, если шаблон не установлен,
# а PowerShell 5.1 превращает вывод native-команды в stderr в ошибку.
$null = & cmd /c "dotnet new uninstall ""$contentDirectory"" >nul 2>&1"
dotnet new install $contentDirectory 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Не удалось установить шаблон." }

$arguments = @('new', 'avalonia-mvvm-app', '-n', $Name, '-o', (Join-Path $outputRoot $Name), '--skipRestore', 'true')
if ($AllPlatforms) {
    $arguments += @('--Android', 'true', '--iOS', 'true', '--Browser', 'true')
}

Write-Host ""
Write-Host "Создание решения: dotnet $($arguments -join ' ')"
& dotnet @arguments
if ($LASTEXITCODE -ne 0) { throw "Не удалось создать решение по шаблону." }

$solutionDirectory = Join-Path $outputRoot $Name

Write-Host ""
Write-Host "Проверка подстановки имени…"
$leftovers = Get-ChildItem $solutionDirectory -Recurse -File |
             Where-Object { $_.Extension -in '.cs', '.axaml', '.csproj', '.sln' } |
             Select-String -Pattern 'AvaloniaMvvmApp' -List
if ($leftovers) {
    Write-Host "НАЙДЕНЫ НЕЗАМЕНЁННЫЕ ВХОЖДЕНИЯ 'AvaloniaMvvmApp':" -ForegroundColor Red
    $leftovers | ForEach-Object { Write-Host "    $($_.Path)" }
    exit 1
}
Write-Host "  имя подставлено везде"

Write-Host ""
Write-Host "Проверка имён, которые подстановка не должна затрагивать…"
# Эти имена ищутся внешними средствами по строковому совпадению, поэтому
# подстановка sourceName обязана их пропустить. Однажды уже ломалось:
# BuildAvaloniaApp превратился в BuildAvaloniaMvvmApp и перестал находиться
# конструктором предварительного просмотра разметки.
$required = @(
    @{ Path = "$Name.Desktop\Program.cs"; Pattern = 'public static AppBuilder BuildAvaloniaApp\(\)'; When = $true },
    @{ Path = "$Name.Browser\Program.cs"; Pattern = 'public static AppBuilder BuildAvaloniaApp\(\)'; When = [bool]$AllPlatforms },
    @{ Path = "$Name.iOS\AppDelegate.cs"; Pattern = 'AvaloniaAppDelegate<App>';                      When = [bool]$AllPlatforms }
)
$missing = @()
foreach ($item in $required) {
    if (-not $item.When) { continue }
    $file = Join-Path $solutionDirectory $item.Path
    if (-not (Test-Path $file)) { $missing += "$($item.Path) — файл не создан"; continue }
    if (-not (Select-String -Path $file -Pattern $item.Pattern -Quiet)) {
        $missing += "$($item.Path) — не найдено: $($item.Pattern)"
    }
}
if ($missing) {
    Write-Host "ПОДСТАНОВКА ИСПОРТИЛА ОБЯЗАТЕЛЬНЫЕ ИМЕНА:" -ForegroundColor Red
    $missing | ForEach-Object { Write-Host "    $_" }
    exit 1
}
Write-Host "  обязательные имена на месте"

Write-Host ""
Write-Host "Проверка директив условной компиляции в .sln…"
$slnFile = Join-Path $solutionDirectory "$Name.sln"
$directives = Select-String -Path $slnFile -Pattern '^#(if|else|endif)'
if ($directives) {
    Write-Host "В .sln остались директивы #if/#endif:" -ForegroundColor Red
    $directives | ForEach-Object { Write-Host "    строка $($_.LineNumber): $($_.Line)" }
    exit 1
}
Write-Host "  директивы обработаны"

Write-Host ""
Write-Host "Сборка решения…"
Write-Host ""

dotnet build $slnFile --nologo -v minimal
$buildExitCode = $LASTEXITCODE

Write-Host ""
if ($buildExitCode -eq 0) {
    Write-Host "ПРОВЕРКА ПРОЙДЕНА: решение по шаблону собирается." -ForegroundColor Green
}
else {
    Write-Host "ПРОВЕРКА НЕ ПРОЙДЕНА: решение не собралось (код $buildExitCode)." -ForegroundColor Red
}

if ($KeepOutput) {
    Write-Host "Решение оставлено: $solutionDirectory"
}
else {
    Remove-Item $outputRoot -Recurse -Force -ErrorAction SilentlyContinue
}

exit $buildExitCode
