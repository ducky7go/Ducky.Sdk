#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

/// <summary>
/// Represents the result of a single build step execution
/// </summary>
public class BuildStepResult
{
    public string StepName { get; set; } = string.Empty;
    public StepStatus Status { get; set; }
    public long StartTimeUnix { get; set; }
    public long EndTimeUnix { get; set; }
    public double DurationSeconds => EndTimeUnix - StartTimeUnix;
    public string? ErrorMessage { get; set; }
    public string? StackTrace { get; set; }
    public int ExitCode { get; set; }

    public static BuildStepResult Success(string stepName, long startTimeUnix, long endTimeUnix)
        => new BuildStepResult
        {
            StepName = stepName,
            Status = StepStatus.Success,
            StartTimeUnix = startTimeUnix,
            EndTimeUnix = endTimeUnix,
            ExitCode = 0
        };

    public static BuildStepResult Failed(string stepName, long startTimeUnix, long endTimeUnix, string errorMessage,
        string? stackTrace = null, int exitCode = 1)
        => new BuildStepResult
        {
            StepName = stepName,
            Status = StepStatus.Failed,
            StartTimeUnix = startTimeUnix,
            EndTimeUnix = endTimeUnix,
            ErrorMessage = errorMessage,
            StackTrace = stackTrace,
            ExitCode = exitCode
        };

    public static BuildStepResult Skipped(string stepName)
        => new BuildStepResult
        {
            StepName = stepName,
            Status = StepStatus.Skipped,
            StartTimeUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            EndTimeUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ExitCode = 36524
        };

    /// <summary>
    /// Gets a formatted string representation of the duration
    /// </summary>
    public string GetFormattedDuration()
    {
        if (Status == StepStatus.Skipped)
            return "skipped";

        var totalSeconds = DurationSeconds;
        if (totalSeconds < 1.0)
            return $"{totalSeconds * 1000:F0}ms";
        else
            return $"{totalSeconds:F1}s";
    }
}

/// <summary>
/// Status of a build step execution
/// </summary>
public enum StepStatus
{
    Success,
    Failed,
    Skipped
}

/// <summary>
/// Provides comprehensive tracking and reporting for build step executions
/// Persists alongside BuildContext as buildResult.json
/// </summary>
public class BuildResult
{
    public required string ProjectDirectory { get; init; }
    public List<BuildStepResult> StepResults { get; private set; } = new();

    public bool IsComplete => StepResults.Count > 0;
    public TimeSpan TotalDuration
    {
        get
        {
            if (StepResults.Count == 0)
                return TimeSpan.Zero;

            // Calculate duration based on actual step execution times
            var firstStep = StepResults.OrderBy(s => s.StartTimeUnix).First();
            var lastStep = StepResults.OrderBy(s => s.EndTimeUnix).Last();

            if (firstStep.StartTimeUnix <= 0 || lastStep.EndTimeUnix <= 0)
                return TimeSpan.Zero;

            var duration = lastStep.EndTimeUnix - firstStep.StartTimeUnix;

            // If duration is unreasonable (more than 1 day), assume it's corrupted data
            if (duration > 86400 || duration < 0) // 1 day in seconds
                return TimeSpan.Zero;

            return TimeSpan.FromSeconds(duration);
        }
    }

    public int SuccessfulSteps => StepResults.Count(r => r.Status == StepStatus.Success);
    public int FailedSteps => StepResults.Count(r => r.Status == StepStatus.Failed);
    public int SkippedSteps => StepResults.Count(r => r.Status == StepStatus.Skipped);
    public int TotalSteps => StepResults.Count;

    public bool HasFailures => FailedSteps > 0;
    public bool OverallSuccess => IsComplete && !HasFailures;

    /// <summary>
    /// Starts tracking a new build step
    /// </summary>
    public void StartStep(string stepName)
    {
        // Remove any existing result for this step (in case of retry)
        StepResults.RemoveAll(r => r.StepName == stepName);
    }

    /// <summary>
    /// Records a successful step execution
    /// </summary>
    public void RecordSuccess(string stepName, long startTimeUnix, long endTimeUnix)
    {
        StepResults.RemoveAll(r => r.StepName == stepName);
        StepResults.Add(BuildStepResult.Success(stepName, startTimeUnix, endTimeUnix));
    }

    /// <summary>
    /// Records a failed step execution
    /// </summary>
    public void RecordFailure(string stepName, long startTimeUnix, long endTimeUnix, string errorMessage,
        string? stackTrace = null, int exitCode = 1)
    {
        StepResults.RemoveAll(r => r.StepName == stepName);
        StepResults.Add(BuildStepResult.Failed(stepName, startTimeUnix, endTimeUnix, errorMessage, stackTrace, exitCode));
    }

    /// <summary>
    /// Records a skipped step
    /// </summary>
    public void RecordSkipped(string stepName)
    {
        StepResults.RemoveAll(r => r.StepName == stepName);
        StepResults.Add(BuildStepResult.Skipped(stepName));
    }

    /// <summary>
    /// Gets the path to the BuildResult JSON file for a project
    /// </summary>
    public static string GetResultFilePath(string projectDirectory)
    {
        return Path.Combine(projectDirectory, "obj", "ducky-build-result.json");
    }

    /// <summary>
    /// Loads BuildResult from project directory, or creates a new one
    /// </summary>
    public static BuildResult LoadOrCreate(string projectDirectory)
    {
        var resultPath = GetResultFilePath(projectDirectory);

        if (File.Exists(resultPath))
        {
            try
            {
                var json = File.ReadAllText(resultPath);
                var result = FromJson(json);
                if (result != null && result.ProjectDirectory == projectDirectory)
                {
                    return result;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BuildResult][WARN] Failed to load existing result: {ex.Message}");
            }
        }

        // Create new BuildResult
        var newResult = new BuildResult
        {
            ProjectDirectory = projectDirectory
        };

        Console.WriteLine($"[BuildResult] Created new result for: {projectDirectory}");
        return newResult;
    }

    /// <summary>
    /// Saves the BuildResult to JSON file
    /// </summary>
    public void Save()
    {
        try
        {
            var resultPath = GetResultFilePath(ProjectDirectory);
            var directory = Path.GetDirectoryName(resultPath);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = ToJson();
            File.WriteAllText(resultPath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BuildResult][WARN] Failed to save result: {ex.Message}");
        }
    }

    /// <summary>
    /// Serializes the result to JSON
    /// </summary>
    public string ToJson() => JsonConvert.SerializeObject(this, Formatting.Indented);

    /// <summary>
    /// Creates a BuildResult from JSON string
    /// </summary>
    public static BuildResult? FromJson(string json)
    {
        try
        {
            return JsonConvert.DeserializeObject<BuildResult>(json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BuildResult][ERROR] Deserialization failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Gets a summary of the build results
    /// </summary>
    public string GetSummary()
    {
        var duration = IsComplete ? TotalDuration.TotalSeconds.ToString("F1") : "ongoing";
        return $"Build: {SuccessfulSteps}✅ {FailedSteps}❌ {SkippedSteps}⏭️ (Duration: {duration}s)";
    }
}

/// <summary>
/// Exit code for script libraries that are intentionally skipped
/// This code is used to distinguish skip status from success (0) and failure (non-zero)
/// </summary>
public const int SkipExitCode = 36524;

/// <summary>
/// Utilities for BuildResult operations
/// </summary>
public static class BuildResultUtils
{
    /// <summary>
    /// Executes a function and records the result in BuildResult
    /// </summary>
    public static int ExecuteAndRecord(BuildResult buildResult, string stepName, Func<int> action)
    {
        var startTimeUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        buildResult.StartStep(stepName);

        try
        {
            var exitCode = action();
            var endTimeUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var duration = endTimeUnix - startTimeUnix;

            if (exitCode == 0)
            {
                buildResult.RecordSuccess(stepName, startTimeUnix, endTimeUnix);
                Console.WriteLine(
                    $"[BuildResult] ✅ {stepName} completed successfully in {duration:F1}s");
            }
            else if (exitCode == SkipExitCode)
            {
                buildResult.RecordSkipped(stepName);
                Console.WriteLine($"[BuildResult] ⏭️ {stepName} was skipped");
            }
            else
            {
                buildResult.RecordFailure(stepName, startTimeUnix, endTimeUnix, $"Exit code: {exitCode}");
                Console.WriteLine($"[BuildResult] ❌ {stepName} failed with exit code {exitCode}");
            }

            return exitCode;
        }
        catch (Exception ex)
        {
            var endTimeUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            buildResult.RecordFailure(stepName, startTimeUnix, endTimeUnix, ex.Message, ex.StackTrace);

            Console.WriteLine($"[BuildResult] ❌ {stepName} failed with exception: {ex.Message}");
            return 1;
        }
        finally
        {
            buildResult.Save();
        }
    }

    /// <summary>
    /// Records a skipped step
    /// </summary>
    public static void RecordSkipped(BuildResult buildResult, string stepName)
    {
        buildResult.RecordSkipped(stepName);
        buildResult.Save();
        Console.WriteLine($"[BuildResult] ⏭️ {stepName} was skipped");
    }
}
