//using System.Diagnostics;
//using System.Net;
//using System.Text;
//using Api.Models;
//using Xunit;

//namespace Tests;

//public class RealDockerTests(RealDockerTestClass factory) : IClassFixture<RealDockerTestClass>
//{
//    private readonly HttpClient _client = factory.CreateClient();

//    [Fact]
//    public async Task ValidRequest_Returns200_AndStartsContainer()
//    {
//        var projectName = $"test-docker-{Guid.NewGuid():N}";
//        var projectDir = Path.Combine(factory.TestProjectsDir, projectName);
//        Directory.CreateDirectory(projectDir);

//        File.WriteAllText(Path.Combine(projectDir, "docker-compose.yml"),
//            $$"""
//            services:
//              app:
//                image: nginx:$${TAG}
//                labels:
//                  - test.deployer.project={{{projectName}}}
//            """);

//        try
//        {
//            var body = new DeployRequest
//            {
//                Project = projectName,
//                Environment = "dev",
//                Tag = "latest",
//            };
//            var content = new StringContent(
//                System.Text.Json.JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
//            var response = await _client.PostAsync("/deploy", content);

//            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

//            var result = await RunProcess("docker",
//                $"ps --filter \"label=test.deployer.project={projectName}\" --format '{{{{.Names}}}}'");
//            Assert.Equal(0, result.ExitCode);
//            var names = result.Stdout.Trim().Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
//            Assert.NotEmpty(names);
//        }
//        finally
//        {
//            await CleanupContainers(projectName);
//            CleanupDirectory(projectDir);
//        }
//    }

//    static async Task CleanupContainers(string labelValue)
//    {
//        try
//        {
//            var listResult = await RunProcess("docker",
//                $"ps -q --filter \"label=test.deployer.project={labelValue}\"");
//            if (listResult.ExitCode == 0)
//            {
//                var ids = listResult.Stdout.Trim().Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
//                foreach (var id in ids)
//                {
//                    _ = await RunProcess("docker", $"stop {id}");
//                    _ = await RunProcess("docker", $"rm {id}");
//                }
//            }
//        }
//        catch { }
//    }

//    static void CleanupDirectory(string directory)
//    {
//        try { Directory.Delete(directory, true); }
//        catch { }
//    }

//    static async Task<ProcessResult> RunProcess(string fileName, string arguments)
//    {
//        using var process = Process.Start(new ProcessStartInfo
//        {
//            FileName = fileName,
//            Arguments = arguments,
//            RedirectStandardOutput = true,
//            RedirectStandardError = true,
//            UseShellExecute = false,
//            CreateNoWindow = true,
//        }) ?? throw new InvalidOperationException($"Failed to start process: {fileName} {arguments}");

//        process.WaitForExit(60_000);
//        var stdout = await process.StandardOutput.ReadToEndAsync();
//        var stderr = await process.StandardError.ReadToEndAsync();

//        return new ProcessResult
//        {
//            ExitCode = process.ExitCode,
//            Stdout = stdout,
//            Stderr = stderr
//        };
//    }
//}
