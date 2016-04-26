param(
    [String] $majorMinor = "0.0",  # 2.0
    [String] $patch = "0",         # $env:APPVEYOR_BUILD_VERSION
    [String] $customLogger = "",   # C:\Program Files\AppVeyor\BuildAgent\Appveyor.MSBuildLogger.dll
    [Switch] $notouch
)

function Set-AssemblyVersions($informational, $assembly)
{
    (Get-Content assets/CommonAssemblyInfo.cs) |
        ForEach-Object { $_ -replace """1.0.0.0""", """$assembly""" } |
        ForEach-Object { $_ -replace """1.0.0""", """$informational""" } |
        ForEach-Object { $_ -replace """1.1.1.1""", """$assembly""" } |
        Set-Content assets/CommonAssemblyInfo.cs
}

function Install-NuGetPackages()
{
    nuget restore serilog-sinks-mongodb.sln
}

function Invoke-MSBuild($solution, $customLogger)
{
    if ($customLogger)
    {
        msbuild "$solution" /verbosity:minimal /p:Configuration=Release /logger:"$customLogger"
    }
    else
    {
        msbuild "$solution" /verbosity:minimal /p:Configuration=Release
    }
}

function Invoke-NuGetPackProj($csproj)
{
    nuget pack -Prop Configuration=Release -Symbols $csproj
}

function Invoke-NuGetPackSpec($nuspec, $version)
{
    nuget pack $nuspec -Version $version -OutputDirectory ..\..\
}

function Invoke-NuGetPack($version)
{
    ls src/**/*.csproj |
        Where-Object { -not ($_.Name -like "*net40*") } |
        ForEach-Object { Invoke-NuGetPackProj $_ }
}

function Invoke-Build($majorMinor, $patch, $customLogger, $notouch)
{
    $package="$majorMinor.$patch"

    Write-Output "Building Serilog.Sinks.MongoDB $package"

    if (-not $notouch)
    {
        $assembly = "$majorMinor.0.0"

        Write-Output "Assembly version will be set to $assembly"
        Set-AssemblyVersions $package $assembly
    }

    Install-NuGetPackages
    
    Invoke-MSBuild "serilog-sinks-mongodb.sln" $customLogger

    Invoke-NuGetPack $package
}

$ErrorActionPreference = "Stop"
Invoke-Build $majorMinor $patch $customLogger $notouch
