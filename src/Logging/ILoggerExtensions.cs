namespace Api.Logging;

public static partial class ILoggerExtensions
{
    // DeploymentService
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Deploying {image} to {project}/{environment}.")]
    public static partial void LogDeploying(this ILogger logger, string image, string project, string environment);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Extracting .env for {project}/{environment} from KeePass.")]
    public static partial void LogExtractingEnv(this ILogger logger, string project, string environment);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Pulling image: {image}.")]
    public static partial void LogPullingImage(this ILogger logger, string image);

    [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Image pulled successfully.")]
    public static partial void LogImagePulled(this ILogger logger);

    [LoggerMessage(EventId = 5, Level = LogLevel.Information, Message = "Running docker compose in {tempDir}.")]
    public static partial void LogRunningCompose(this ILogger logger, string tempDir);

    [LoggerMessage(EventId = 6, Level = LogLevel.Information, Message = "Successfully deployed tag {tag} to {project}/{environment}.")]
    public static partial void LogDeploySuccess(this ILogger logger, string tag, string project, string environment);

    [LoggerMessage(EventId = 7, Level = LogLevel.Error, Message = "Compose up failed: {stderr}.")]
    public static partial void LogComposeFailed(this ILogger logger, string stderr);

    [LoggerMessage(EventId = 8, Level = LogLevel.Warning, Message = "Failed to remove temp dir {tempDir}.")]
    public static partial void LogTempDirCleanupFailed(this ILogger logger, string tempDir, Exception ex);

    // KeePassEnvService
    [LoggerMessage(EventId = 9, Level = LogLevel.Information, Message = "Wrote .env to {targetDir} for {project}/{environment}.")]
    public static partial void LogEnvWritten(this ILogger logger, string targetDir, string project, string environment);

    [LoggerMessage(EventId = 10, Level = LogLevel.Warning, Message = "Failed to cleanup .env from {path}.")]
    public static partial void LogEnvCleanupFailed(this ILogger logger, string path, Exception ex);

    [LoggerMessage(EventId = 11, Level = LogLevel.Warning, Message = "keepassxc-cli exit={exitCode} for {group}/{entry}/{attachment}: {stderr}.")]
    public static partial void LogKeePassCliFailed(this ILogger logger, int exitCode, string group, string entry, string attachment, string stderr);

    // ExceptionHandlerExtensions
    [LoggerMessage(EventId = 12, Level = LogLevel.Error, Message = "Unhandled {exceptionType}: {message} at {path}.")]
    public static partial void LogUnhandledException(this ILogger logger, string exceptionType, string message, string path, Exception ex);
}
