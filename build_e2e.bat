@echo off
echo === Building E2E Tests ===
C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe /nologo /out:E2ETest.exe /reference:System.Windows.Forms.dll E2E\E2ETests.cs
if errorlevel 1 (
    echo [FAIL] E2E test compilation failed
    exit /b 1
)
echo [PASS] E2ETest.exe built successfully
