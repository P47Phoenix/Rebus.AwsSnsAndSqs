#tool nuget:?package=NUnit.ConsoleRunner&version=3.4.0
//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("Configuration", "Release");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////
var outputDir = Directory("") + Directory(configuration);  // The output directory the build artefacts saved too

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    DotNetCoreClean("./Rebus.AwsSnsAndSqs.sln", new DotNetCoreCleanSettings()
    {
        Configuration = configuration
    });
});


Task("Build")
    .IsDependentOn("Clean")
    .Does(() =>
{
    DotNetCoreBuild("./Rebus.AwsSnsAndSqs.sln", new DotNetCoreBuildSettings()
    {
        Configuration = configuration
    });
});

Task("Pack")
    .IsDependentOn("Build")
    .Does(() =>
    {
        DotNetCorePack("./Rebus.AwsSnsAndSqs.sln", new DotNetCorePackSettings
        {
            OutputDirectory = outputDir,
            NoBuild = true,
            IncludeSource = true,
            IncludeSymbols = true
        });
    });

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Pack");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);