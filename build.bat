@echo off
echo using System.Reflection; >IharBury.NHibernate\Properties\AssemblyVersion.cs
echo [assembly: AssemblyVersion("%1.0")] >>IharBury.NHibernate\Properties\AssemblyVersion.cs
echo [assembly: AssemblyFileVersion("%1.0")] >>IharBury.NHibernate\Properties\AssemblyVersion.cs
echo [assembly: AssemblyInformationalVersion("%1")] >>IharBury.NHibernate\Properties\AssemblyVersion.cs
nuget pack IharBury.NHibernate -Build -Prop Configuration=Release -Symbols
echo. >IharBury.NHibernate\Properties\AssemblyVersion.cs
