@echo off
echo Building FastClick...

dotnet restore
if %ERRORLEVEL% neq 0 (
    echo Failed to restore packages
    pause
    exit /b 1
)

dotnet build --configuration Release
if %ERRORLEVEL% neq 0 (
    echo Build failed
    pause
    exit /b 1
)

echo.
echo Build completed successfully!
echo Output: bin\Release\net8.0-windows\FastClick.exe
echo.
echo To run the application, execute it as Administrator.
pause