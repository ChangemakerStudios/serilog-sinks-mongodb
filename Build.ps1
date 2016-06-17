$root = $(Get-Item $($MyInvocation.MyCommand.Path)).DirectoryName

function Get-DnxVersion
{
    $globalJson = Join-Path $PSScriptRoot "global.json"
    $jsonData = Get-Content -Path $globalJson -Raw | ConvertFrom-JSON
    return $jsonData.sdk.version
}

function Restore-Packages
{
    param([string] $DirectoryName)
    & dotnet restore ("""" + $DirectoryName + """")
}

function Build-Projects
{
    param($Directory, $pack)
     
    $DirectoryName = $Directory.DirectoryName
    $artifactsFolder = join-path $root "artifacts"
    $projectsFolder = join-path $artifactsFolder $Directory.Name 
    $buildFolder = join-path $projectsFolder "testbin"
    $packageFolder = join-path $projectsFolder "packages"
	$framework = "net46"
     
    & dotnet build ("""" + $DirectoryName + """") --configuration Release --output $buildFolder --framework $framework; if($LASTEXITCODE -ne 0) { exit 1 }
    
    if($pack){
        & dotnet pack ("""" + $DirectoryName + """") --configuration Release --output $packageFolder; if($LASTEXITCODE -ne 0) { exit 1 }
    }
}
 
function Test-Projects
{
    param([string] $DirectoryName)
    & dotnet -p ("""" + $DirectoryName + """") test; if($LASTEXITCODE -ne 0) { exit 2 }
}

function Remove-PathVariable
{
    param([string] $VariableToRemove)
    $path = [Environment]::GetEnvironmentVariable("PATH", "User")
    $newItems = $path.Split(';') | Where-Object { $_.ToString() -inotlike $VariableToRemove }
    [Environment]::SetEnvironmentVariable("PATH", [System.String]::Join(';', $newItems), "User")
    $path = [Environment]::GetEnvironmentVariable("PATH", "Process")
    $newItems = $path.Split(';') | Where-Object { $_.ToString() -inotlike $VariableToRemove }
    [Environment]::SetEnvironmentVariable("PATH", [System.String]::Join(';', $newItems), "Process")
}

Push-Location $PSScriptRoot

$dnxVersion = Get-DnxVersion

# Clean
if(Test-Path .\artifacts) { Remove-Item .\artifacts -Force -Recurse }

# Package restore
Get-ChildItem -Path . -Filter *.xproj -Recurse | ForEach-Object { Restore-Packages $_.DirectoryName }

# Set build number
$env:DNX_BUILD_VERSION = @{ $true = $env:APPVEYOR_BUILD_NUMBER; $false = 1 }[$env:APPVEYOR_BUILD_NUMBER -ne $NULL];
Write-Host "Build number: " $env:DNX_BUILD_VERSION

# Build/package
Get-ChildItem -Path .\src -Filter *.xproj -Recurse | ForEach-Object { Build-Projects $_ $true }
Get-ChildItem -Path .\test -Filter *.xproj -Recurse | ForEach-Object { Build-Projects $_ $false }

# Test
Get-ChildItem -Path .\test -Filter *.xproj -Recurse | ForEach-Object { Test-Projects $_.DirectoryName }

Pop-Location
