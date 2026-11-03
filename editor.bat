@echo off
rem Abre o projeto direto no editor do Godot (depois é só apertar F5 para jogar).
set "GODOT=%LOCALAPPDATA%\Microsoft\WinGet\Packages\GodotEngine.GodotEngine.Mono_Microsoft.Winget.Source_8wekyb3d8bbwe\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe"
start "" "%GODOT%" --path "%~dp0game" --editor
