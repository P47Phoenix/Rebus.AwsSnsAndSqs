#tool nuget:?package=NUnit.ConsoleRunner&version=3.4.0
//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("Configuration", "Release");

var buildNumber = EnvironmentVariable("CI_PIPELINE_IID");
var isProtected = EnvironmentVariable("CI_COMMIT_REF_PROTECTED");
var nugetApiKey = EnvironmentVariable("NUGET_API_KEY");
var branch = EnvironmentVariable("CI_COMMIT_BRANCH");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

var outputDir = Directory("./nupkgs");  // The output directory the build artefacts saved too
var solution = "./Rebus.AwsSnsAndSqs.sln";
var projectToPackage = "./Rebus.AwsSnsAndSqs/Rebus.AwsSnsAndSqs.csproj";
var packageName = "Rebus.AwsSnsAndSqs";
string packageVersion = null;

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Setup")
    .Does(() =>
    {
        if(string.IsNullOrWhiteSpace(branch))
        {
            Information("Setup for local build");
            buildNumber = "1";
            isProtected = "False";
            nugetApiKey = "Not for your eyes";
            branch = "Not in gitlab";
        }

        if(branch.Equals("master", StringComparison.InvariantCultureIgnoreCase))
        {
            packageVersion  = $"5.4.{buildNumber}";
        }
        else
        {
            var cleanBranchName = branch.Replace(" ", "-");
            packageVersion  = $"5.4.{buildNumber}-{cleanBranchName}";
        }
    });

Task("Clean")
    .IsDependentOn("Setup")
    .Does(() =>
{

    DotNetCoreClean(solution, new DotNetCoreCleanSettings()
    {
        Configuration = configuration
    });
});


Task("Build")
    .IsDependentOn("Clean")
    .Does(() =>
{
    DotNetCoreBuild(solution, new DotNetCoreBuildSettings()
    {
        Configuration = configuration
    });
});
Task("Pack")
    .IsDependentOn("Build")
    .Does(() =>
    {
        DotNetCorePack(projectToPackage, new DotNetCorePackSettings
        {
            Configuration = configuration
            OutputDirectory = outputDir,
            NoBuild = true,
            IncludeSource = true,
            IncludeSymbols = true,
            ArgumentCustomization = (args) => args
                .Append($"-p:\"PackageVersion={packageVersion}\"")
        });
    });

Task("Push")
    .IsDependentOn("Pack")
    .Does(() =>
    {
        if(isProtected.Equals(bool.TrueString, StringComparison.InvariantCultureIgnoreCase))
        {
            Information("In protected branch");
            NuGetPush($"{outputDir}/{packageName}.{packageVersion}.nupkg", new NuGetPushSettings {
                Source = "https://api.nuget.org/v3/index.json",
                ApiKey = nugetApiKey
            });
        }
        else
        {
            Warning("Branch is not protect. Package will not be pushed to nuget");
        }
    });

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Push");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);