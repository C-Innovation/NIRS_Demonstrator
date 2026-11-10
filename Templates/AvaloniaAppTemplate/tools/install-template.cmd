@echo off
rem ---------------------------------------------------------------------------
rem Installs the Avalonia MVVM App template into Visual Studio.
rem
rem Use this wrapper when the machine execution policy is AllSigned or
rem Restricted: -ExecutionPolicy Bypass applies to THIS PowerShell process
rem only and changes no machine or user setting.
rem
rem Any arguments are forwarded, e.g.:  install-template.cmd -ArchiveOnly
rem ---------------------------------------------------------------------------

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-Template.ps1" %*
exit /b %ERRORLEVEL%
