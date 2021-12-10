#addin "nuget:?package=Cake.Docker&version=1.0.0"
#addin "nuget:?package=Cake.Coverlet&version=2.5.4"
#addin nuget:?package=Cake.Sonar&version=1.1.25
#tool dotnet:?package=dotnet-reportgenerator-globaltool&version=4.6.7

#tool "nuget:?package=MSBuild.SonarQube.Runner.Tool&version=4.8.0"
#module nuget:?package=Cake.DotNetTool.Module&version=0.4.0
#addin "nuget:?package=Cake.Docker&version=0.10.0"
#addin nuget:?package=Cake.Git&version=1.0.1

///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var publishDir = Argument ("publishDir", "./publish");
var solutionDir = System.IO.Directory.GetCurrentDirectory ();
var dockerHubUrl = "ghcr.io/ilkerhalil";
var projectName = "weather-forecast-sample";
var imageName="weather-forecast";
var coverletDirectory = "./coverlet";
FilePath filePath = "./coverlet/results-Api.UnitTests.xml";
var repositoryDirectoryPath = DirectoryPath.FromString(".");
var currentBranch = GitBranchCurrent(repositoryDirectoryPath);

string version="";
///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////
Task ("Clean")
    .Does (() => {
        var settings = new DeleteDirectorySettings {
            Recursive = true,
            Force = true
        };
        if (DirectoryExists (publishDir)) {
            CleanDirectory (publishDir);
            DeleteDirectory (publishDir, settings);
        }
        if(DirectoryExists(coverletDirectory))
        {
            CleanDirectory(coverletDirectory);
        }
        var binDirs = GetDirectories ("./src/**/bin");
        var objDirs = GetDirectories ("./src/**/obj");
        CleanDirectories (binDirs);
        CleanDirectories (objDirs);
        DeleteDirectories (binDirs, settings);
        DeleteDirectories (objDirs, settings);
        DotNetCoreClean (".");
    });

Task ("Restore")
    .Does (() => {
        DotNetCoreRestore (".");
    });

Task ("Build")
    .IsDependentOn ("Clean")
    .IsDependentOn ("Restore")
    .Does (() => {
        var settings = new DotNetCoreBuildSettings {
        Configuration = configuration
        };
        DotNetCoreBuild (".", settings);
    });
Task("Run-Tests")
    .Does(() =>
{
    var settings = new DotNetCoreTestSettings
    {
        Configuration = configuration,
        NoBuild=true
    };

    var coverletSettings = new CoverletSettings();
    coverletSettings.CollectCoverage =true;
    coverletSettings.CoverletOutputFormat= CoverletOutputFormat.opencover;
    coverletSettings.CoverletOutputDirectory = "./coverlet";
    coverletSettings.ExcludeByAttribute=new List<string>(){"Obsolete","GeneratedCodeAttribute","CompilerGeneratedAttribute"};
    var files = GetFiles("./test/**/*.csproj");
    Information($"Found {files.Count} test project! ");

    var reportFileName = string.Empty;
    foreach(var file in files){
        reportFileName= $"results-{file.GetFilenameWithoutExtension()}.xml";
        Information($"Generate report file {reportFileName}");
        coverletSettings.CoverletOutputName = reportFileName;
        DotNetCoreTest(file.FullPath,settings,coverletSettings);
    }
    var generatorSettings = new ReportGeneratorSettings();
    generatorSettings.ReportTypes= new ReportGeneratorReportType[]{ReportGeneratorReportType.SonarQube};

    ReportGenerator(filePath,coverletDirectory,generatorSettings);
});
Task("Sonar-Begin")
.Does(()=> {
   var testReportPath = Directory("./coverlet/SonarQube.xml");
   var sonarqubeApiKey = EnvironmentVariable("SONAR_API_KEY");
   var sonarBeginSettings= new SonarBeginSettings();
   sonarBeginSettings.Key=projectName;
   sonarBeginSettings.Organization="ilkerhalil";
   sonarBeginSettings.Url= "https://sonarcloud.io";
   sonarBeginSettings.Login = sonarqubeApiKey;
   sonarBeginSettings.ArgumentCustomization = args => args.Append($"/d:sonar.coverageReportPaths=\"{testReportPath}\"");
   SonarBegin(sonarBeginSettings);

});
Task("Sonar-End")
    .Does(() =>
{
    var sonarqubeApiKey = EnvironmentVariable("SONAR_API_KEY");
    SonarEnd(new SonarEndSettings{
        Login = sonarqubeApiKey
     });
});
Task("Sonar")
    .IsDependentOn("Sonar-Begin")
    .IsDependentOn("Build")
    .IsDependentOn("Run-Tests")
    .IsDependentOn("Sonar-End")
    .Does(()=> {
        Information("Finished Sonar");

    }
);

Task("Publish")
.IsDependentOn("Build")
.Does(()=>{
    var settings=  new DotNetCorePublishSettings
    {
        Configuration = configuration,
        OutputDirectory = publishDir,
        ArgumentCustomization = args => args.Append($"/p:Version={version}")
    };
    DotNetCorePublish("./src/Api/Api.csproj",settings);
});

Task("Docker-Build")
.IsDependentOn("Get-Version")
.Does(()=>{
    var dockerImageName =$"{dockerHubUrl}/{imageName}:{version}";
    DockerImageBuildSettings settings = new DockerImageBuildSettings();
    settings.Rm = true;
    settings.NoCache = true;
    settings.DisableContentTrust = true;
    settings.Tag = new string [] {dockerImageName};
    DockerBuild(settings,".");
    if(currentBranch.CanonicalName == "master")
    {
        dockerImageName =$"{dockerHubUrl}/{imageName}:latest";
        settings = new DockerImageBuildSettings();
        settings.Rm = true;
        settings.NoCache = true;
        settings.DisableContentTrust = true;
        settings.Tag = new string [] {dockerImageName};
        DockerBuild(settings,".");
    }
});

Task("Docker-Push")
.IsDependentOn("Get-Version")
.Does(()=>
{
    var dockerImageName =$"{dockerHubUrl}/{imageName}:{version}";
    if(!BuildSystem.IsLocalBuild){
        var userName = EnvironmentVariable("DOCKER_REGISTRY_USERNAME");
        var password = EnvironmentVariable("DOCKER_REGISTRY_PASSWORD");
        DockerLogin(userName,password,dockerHubUrl);
    }
    DockerPush(dockerImageName);
    if(currentBranch.CanonicalName == "master")
    {
        dockerImageName =$"{dockerHubUrl}/{imageName}:latest";
        DockerPush(dockerImageName);
    }

});
Task("Get-Version")
    .Does(() =>
{
    version = System.IO.File.ReadAllText("./version.md");
    version = System.Text.RegularExpressions.Regex.Replace(version, @"\t|\n|\r", "");
    System.IO.File.Delete("./version.md");
    System.IO.File.WriteAllText("./version.md",version);
    Information($"Current Version {version}");

});
Task("Helm-Deploy")
.IsDependentOn("Get-Version")
.Does(() =>
{
    var userName = EnvironmentVariable("DOCKER_REGISTRY_USERNAME");
    var password = EnvironmentVariable("DOCKER_REGISTRY_PASSWORD");
    var mail = "ilkerhalil@gmail.com";
    var targetNameSpace =  currentBranch.CanonicalName.Contains("development")?"weather-forecast-beta":"weather-forecast";
    var parameterBuilder = new StringBuilder();
    parameterBuilder.Append(" weather-forecast ./chart --install --insecure-skip-tls-verify --create-namespace --wait ");
    parameterBuilder.Append($"--namespace {targetNameSpace} ");
    parameterBuilder.Append($"--set image.tag={version} ");
    parameterBuilder.Append($"--set imageCredentials.username={userName} ");
    parameterBuilder.Append($"--set imageCredentials.password={password} ");
    parameterBuilder.Append($"--set imageCredentials.email={mail} ");
    var parameters = parameterBuilder.ToString();
    var result = StartProcess("helm",$"upgrade {parameters}");
    if(result!=0)
    {
        throw new Exception($"Message {parameters}");
    }
});

RunTarget(target);