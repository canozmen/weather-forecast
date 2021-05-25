#addin "nuget:?package=Cake.Docker&version=1.0.0"
#addin "nuget:?package=Cake.Coverlet&version=2.5.4"
#addin nuget:?package=Cake.Sonar&version=1.1.22
#tool dotnet:?package=dotnet-reportgenerator-globaltool&version=4.6.7

#tool "nuget:?package=MSBuild.SonarQube.Runner.Tool&version=4.6.0"
#module nuget:?package=Cake.DotNetTool.Module&version=0.4.0


///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var publishDir = Argument ("publishDir", "./publish");
var solutionDir = System.IO.Directory.GetCurrentDirectory ();
var dockerHubUrl = "ghcr.io";
var projectName = "weather-forecast-sample";
var imageName="weather-forecast-api";
var coverletDirectory = "./coverlet";
FilePath filePath = "./coverlet/results-Api.UnitTests.xml";
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

RunTarget(target);