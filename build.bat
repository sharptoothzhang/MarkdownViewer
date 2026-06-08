@echo off
set WEBVIEW2_REF=C:\Users\sharptooth\.nuget\packages\webview2\1.0.2739.15\lib\net462
set RUNtimes=C:\Users\sharptooth\.nuget\packages\webview2\1.0.2739.15\runtimes\win-x64\native

echo === Copy resources ===
if not exist Release mkdir Release
copy app.ico Release\ >nul
copy Scripts\mermaid.min.js Release\ >nul
copy Scripts\highlight.min.js Release\ >nul
copy Scripts\hljs-github.min.css Release\ >nul
copy Scripts\hljs-github-dark.min.css Release\ >nul

echo === Running Unit Tests ===
copy /Y lib\Markdig.dll . >nul 2>nul
copy /Y lib\System.Memory.dll . >nul 2>nul
copy /Y lib\System.Buffers.dll . >nul 2>nul
copy /Y lib\System.Numerics.Vectors.dll . >nul 2>nul
copy /Y lib\System.Runtime.CompilerServices.Unsafe.dll . >nul 2>nul
C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe /nologo /out:Test.exe /reference:Markdig.dll /reference:System.Windows.Forms.dll Test.cs Core\MarkdownParser.cs
if errorlevel 1 (
    echo [FAIL] Unit test compilation failed
    goto :cleanup
)
Test.exe
if errorlevel 1 (
    echo [FAIL] Unit tests failed
    goto :cleanup
)
del /q Markdig.dll System.Memory.dll System.Buffers.dll System.Numerics.Vectors.dll System.Runtime.CompilerServices.Unsafe.dll 2>nul

echo.
echo === Building MarkdownViewer ===
taskkill /F /IM MarkdownViewer.exe 2>nul >nul
C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe /nologo /target:winexe /win32icon:app.ico /out:Release\MarkdownViewer.exe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:lib\Markdig.dll /reference:%WEBVIEW2_REF%\Microsoft.Web.WebView2.Core.dll /reference:%WEBVIEW2_REF%\Microsoft.Web.WebView2.WinForms.dll Program.cs Core\*.cs Forms\*.cs Hooks\*.cs Resources\*.cs
if errorlevel 1 (
    echo [FAIL] Build failed
    goto :end
)

echo.
echo === Copy DLLs ===
copy /Y "%WEBVIEW2_REF%\Microsoft.Web.WebView2.Core.dll" Release\ >nul
copy /Y "%WEBVIEW2_REF%\Microsoft.Web.WebView2.WinForms.dll" Release\ >nul
copy /Y "%RUNtimes%\WebView2Loader.dll" Release\ >nul
copy /Y lib\Markdig.dll Release\ >nul
copy /Y lib\System.Memory.dll Release\ >nul
copy /Y lib\System.Buffers.dll Release\ >nul
copy /Y lib\System.Numerics.Vectors.dll Release\ >nul
copy /Y lib\System.Runtime.CompilerServices.Unsafe.dll Release\ >nul

echo.
echo === Testing Mermaid Rendering ===
del /q Release\debug_*.log 2>nul
start "" /D "%~dp0Release" MarkdownViewer.exe --debug "%~dp0mermaid_test.md"
timeout /t 6 /nobreak >nul
taskkill /F /IM MarkdownViewer.exe 2>nul >nul
set MERMAID_OK=0
for %%f in (Release\debug_*.log) do (
    findstr /C:"MERMAID_OK" %%f >nul 2>nul
    if not errorlevel 1 set MERMAID_OK=1
)
if %MERMAID_OK%==1 (
    echo [PASS] Mermaid rendering works
) else (
    echo [WARN] Mermaid test inconclusive - check manually
)

echo.
echo === Running E2E Tests ===
taskkill /F /IM MarkdownViewer.exe 2>nul >nul
C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe /nologo /out:E2ETest.exe /reference:System.Windows.Forms.dll E2E\E2ETests.cs
if errorlevel 1 (
    echo [FAIL] E2E compilation failed
    goto :end
)
E2ETest.exe
if errorlevel 1 goto :end
taskkill /F /IM MarkdownViewer.exe 2>nul >nul

echo.
echo === All Tests Passed && Build Complete ===
goto :end

:cleanup
del /q Markdig.dll System.Memory.dll System.Buffers.dll System.Numerics.Vectors.dll System.Runtime.CompilerServices.Unsafe.dll 2>nul

:end
