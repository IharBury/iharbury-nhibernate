@echo off
nuget restore
IF %ERRORLEVEL% NEQ 0 exit

echo using System.Reflection; >IharBury.NHibernate\Properties\AssemblyVersion.cs
echo [assembly: AssemblyVersion("%1.0")] >>IharBury.NHibernate\Properties\AssemblyVersion.cs
echo [assembly: AssemblyFileVersion("%1.0")] >>IharBury.NHibernate\Properties\AssemblyVersion.cs
echo [assembly: AssemblyInformationalVersion("%1")] >>IharBury.NHibernate\Properties\AssemblyVersion.cs
msbuild IharBury.NHibernate.sln /property:Configuration=Release
set msbuilderrorlevel=%ERRORLEVEL%
echo. >IharBury.NHibernate\Properties\AssemblyVersion.cs
IF %msbuilderrorlevel% NEQ 0 exit

packages\xunit.runner.console.2.3.1\tools\net452\xunit.console.exe IharBury.NHibernate.Tests\bin\Release\IharBury.NHibernate.Tests.dll
IF %ERRORLEVEL% NEQ 0 exit

nuget pack IharBury.NHibernate -Prop Configuration=Release -Symbols
