@echo off
rem ---------------------------------------------------------------------------
rem Expands the template and builds the generated solution.
rem
rem Wrapper for machines whose execution policy is AllSigned or Restricted:
rem -ExecutionPolicy Bypass applies to THIS PowerShell process only.
rem
rem Any arguments are forwarded, e.g.:  test-template.cmd -KeepOutput
rem ---------------------------------------------------------------------------

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Test-Template.ps1" %*
exit /b %ERRORLEVEL%
