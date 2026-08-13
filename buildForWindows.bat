cd src\Mp3FileChecker
dotnet publish -c Release --output publish/ -r win-x64 --self-contained true
@IF ERRORLEVEL 1 (
@ECHO.Publish failed, the publish folder is not up to date.
pause
exit /b 1
)
@ECHO.Build successful. Press any key to exit.
pause
