@echo off
setlocal EnableExtensions
cd /d "%~dp0"

if not exist "cimgui\cimgui.cpp" (
  git clone --depth 1 --branch 1.90.8dock --recurse-submodules https://github.com/cimgui/cimgui.git cimgui
  if errorlevel 1 exit /b 1
)

set "VCVARS="
for /f "usebackq delims=" %%i in (`"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -latest -products * -property installationPath`) do (
  if exist "%%i\VC\Auxiliary\Build\vcvars64.bat" set "VCVARS=%%i\VC\Auxiliary\Build\vcvars64.bat"
)
if not defined VCVARS if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\2022\BuildTools\VC\Auxiliary\Build\vcvars64.bat" (
  set "VCVARS=%ProgramFiles(x86)%\Microsoft Visual Studio\2022\BuildTools\VC\Auxiliary\Build\vcvars64.bat"
)
if not defined VCVARS (
  echo MSVC not found
  exit /b 1
)
call "%VCVARS%"

cl /nologo /LD /EHsc /O2 /MD /std:c++17 /utf-8 ^
  /W3 /wd4244 /wd4305 /D_CRT_SECURE_NO_WARNINGS ^
  /DIMGUI_DISABLE_OBSOLETE_FUNCTIONS=1 ^
  /I cimgui /I cimgui\imgui /I . ^
  cimgui\cimgui.cpp ^
  cimgui\imgui\imgui.cpp ^
  cimgui\imgui\imgui_draw.cpp ^
  cimgui\imgui\imgui_demo.cpp ^
  cimgui\imgui\imgui_widgets.cpp ^
  cimgui\imgui\imgui_tables.cpp ^
  ImGuizmo.cpp ^
  wrapper.cpp ^
  /Fe:cimgui.dll ^
  imm32.lib
if errorlevel 1 exit /b 1

del /q *.obj *.exp 2>nul
echo built %~dp0cimgui.dll
