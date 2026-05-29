@echo off
cd /d "%~dp0"

echo === E2E Tests for MarkdownViewer ===
echo.

if not exist "test.md" (
    echo WARNING: test.md not found, some tests may fail
)

taskkill /F /IM MarkdownViewer.exe 2>nul

echo Compiling E2E tests...
"C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe" /nologo /out:E2ETest.exe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll E2E\E2ETests.cs
if errorlevel 1 (
    echo E2E test compilation failed!
    exit /b 1
)

echo Running E2E tests...
E2ETest.exe
if errorlevel 1 (
    echo E2E tests FAILED!
    exit /b 1
)

echo.
echo === E2E Tests Complete ===
