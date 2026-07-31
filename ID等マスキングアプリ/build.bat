@echo off
REM Builds MaskingTool as a single self-contained exe for win-x64.
REM Requires .NET 8 SDK: https://dotnet.microsoft.com/download

dotnet publish -c Release -r win-x64 --self-contained true

echo.
echo Build finished. MaskingTool.exe is at:
echo   bin\Release\net8.0-windows\win-x64\publish\MaskingTool.exe
echo.
pause
