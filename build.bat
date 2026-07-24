@echo off

set WEBVIEW2_REF=C:\Users\sharptooth\.nuget\packages\webview2\1.0.2739.15\lib\net462

echo === Copy resources ===
if not exist Release mkdir Release
if not exist Release\Resources mkdir Release\Resources
if not exist Release\Resources\css mkdir Release\Resources\css
if not exist Release\Resources\js mkdir Release\Resources\js
copy /Y app.ico Release\ >nul
copy /Y Resources\preview.html Release\Resources\ >nul
copy /Y Resources\help.html Release\Resources\ >nul
copy /Y Resources\css\light.css Release\Resources\css\ >nul
copy /Y Resources\css\outline.css Release\Resources\css\ >nul
copy /Y Resources\js\preview.js Release\Resources\js\ >nul
copy /Y Resources\js\highlight.min.js Release\Resources\js\ >nul
copy /Y Resources\js\mermaid.min.js Release\Resources\js\ >nul

echo === Running Unit Tests ===
copy /Y lib\Markdig.dll . >nul 2>nul
copy /Y lib\System.Memory.dll . >nul 2>nul
copy /Y lib\System.Buffers.dll . >nul 2>nul
copy /Y lib\System.Numerics.Vectors.dll . >nul 2>nul
copy /Y lib\System.Runtime.CompilerServices.Unsafe.dll . >nul 2>nul

C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe /nologo /out:Test.exe /reference:Markdig.dll /reference:System.Windows.Forms.dll Test.cs Core\MarkdownParser.cs
if errorlevel 1 (
    echo [FAIL] Unit test compilation failed
    exit /b 1
)

Test.exe
if errorlevel 1 (
    echo [FAIL] Unit tests failed
    exit /b 1
)

del /q Markdig.dll System.Memory.dll System.Buffers.dll System.Numerics.Vectors.dll System.Runtime.CompilerServices.Unsafe.dll 2>nul

echo.
echo === Building MarkdownViewer ===
taskkill /F /IM MarkdownViewer.exe >nul 2>&1

C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe /nologo /target:winexe /win32icon:app.ico /out:Release\MarkdownViewer.exe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:lib\Markdig.dll /reference:%WEBVIEW2_REF%\Microsoft.Web.WebView2.Core.dll /reference:%WEBVIEW2_REF%\Microsoft.Web.WebView2.WinForms.dll Program.cs Core\*.cs Forms\*.cs Hooks\*.cs
if errorlevel 1 (
    echo [FAIL] Build failed
    exit /b 1
)

echo.
echo === Copy DLLs ===
copy /Y "%WEBVIEW2_REF%\Microsoft.Web.WebView2.Core.dll" Release\ >nul
copy /Y "%WEBVIEW2_REF%\Microsoft.Web.WebView2.WinForms.dll" Release\ >nul
copy /Y lib\WebView2Loader.dll Release\ >nul
copy /Y lib\Markdig.dll Release\ >nul
copy /Y lib\System.Memory.dll Release\ >nul
copy /Y lib\System.Buffers.dll Release\ >nul
copy /Y lib\System.Numerics.Vectors.dll Release\ >nul
copy /Y lib\System.Runtime.CompilerServices.Unsafe.dll Release\ >nul

echo.
echo === All Tests Passed ^&^& Build Complete ===
exit /b 0