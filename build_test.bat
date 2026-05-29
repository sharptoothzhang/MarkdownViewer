@echo off
echo === Building Unit Tests ===
C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe /nologo /out:Test.exe /reference:lib\Markdig.dll /reference:System.Windows.Forms.dll Test.cs Core\MarkdownParser.cs
if errorlevel 1 (
    echo [FAIL] Unit test compilation failed
    exit /b 1
)
echo [PASS] Test.exe built successfully
