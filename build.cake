var target = Argument<string>("Target", "Build");
var configuration = Argument<string>("Configuration", "Debug");

FilePath SolutionFile = new FilePath("SqsPoller.sln").MakeAbsolute(Context.Environment);
FilePath TestProjectFile = "tests/Tests.SqsPoller/Tests.SqsPoller.csproj";
FilePath LambdaProjectFile = "src/SqsPoller/SqsPoller.csproj";

var outputFolder = SolutionFile.GetDirectory().Combine("outputs");

Setup(context => 
{
    CleanDirectory(outputFolder);
});

Task("Build")
    .Does(() =>
{
    var settings = new DotNetCoreBuildSettings
    {
        Configuration = configuration
    };

    DotNetCoreBuild(SolutionFile.FullPath, settings);
});

Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
{
    DotNetCoreTest(TestProjectFile.FullPath);
});

Task("Package")
    .IsDependentOn("Test")
    .Does(() => 
{
    var arguments = new ProcessArgumentBuilder()
                            .Append("package");

    var settings = new DotNetCoreToolSettings
    {
        WorkingDirectory = LambdaProjectFile.GetDirectory()
    };

    DotNetCoreTool(LambdaProjectFile.FullPath, "lambda", arguments, settings);

    MoveFiles("./src/**/bin/**/*.zip", outputFolder);
});

RunTarget(target);