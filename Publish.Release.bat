@echo off

REM Change to the directory containing the batch file
cd /d "%~dp0"

REM Run to delete all files and folders
rmdir /Q /S bin\Release\net8.0\publish\

REM Run dotnet publish to publish your project
dotnet publish -c Release /p:DebugType=None /p:DebugSymbols=false -o ./bin/Release/net8.0/publish/

REM Some files in wwwroot are not copied by dotnet publish
REM Copy the wwwroot directory to the publish directory
REM xcopy /Y /I /E ".\wwwroot" ".\bin\Release\net8.0\publish\wwwroot\"

REM Run to delete all setting files
del bin\Release\net8.0\publish\web.config
del bin\Release\net8.0\publish\log4net.config
del bin\Release\net8.0\publish\appsettings.json
del bin\Release\net8.0\publish\appsettings.Development.json
del bin\Release\net8.0\publish\appsettings.Development.*.json

REM zip folder by 7z
cd bin\Release\net8.0\publish\
"C:\Program Files\7-Zip\7z.exe" a -r ./publish.zip *
start .
