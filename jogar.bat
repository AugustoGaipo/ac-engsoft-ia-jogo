@echo off
rem Abre o jogo (Godot 4.7.2 .NET). Use "jogar.bat teste" para a sala de testes.
set "GODOT=%LOCALAPPDATA%\Microsoft\WinGet\Packages\GodotEngine.GodotEngine.Mono_Microsoft.Winget.Source_8wekyb3d8bbwe\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe"
cd /d "%~dp0"
dotnet build game -v q -nologo || (pause & exit /b 1)
if /i "%1"=="teste" (start "" "%GODOT%" --path game -- --sandbox) else (start "" "%GODOT%" --path game)
