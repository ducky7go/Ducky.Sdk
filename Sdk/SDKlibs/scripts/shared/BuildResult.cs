#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;
using Newtonsoft.Json;

/// <summary>
/// Represents the result of a single build step execution
/// </summary>
public class BuildStepResult
{
    public string StepName { get; set; } = string.Empty;
    public StepStatus Status { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration => EndTime - StartTime;
    public string? ErrorMessage { get; set; }
    public string? StackTrace { get; set; }
    public int ExitCode { get; set; }

    public static BuildStepResult Success(string stepName, DateTime startTime, DateTime endTime)
        => new BuildStepResult
        {
            StepName = stepName,
            Status = StepStatus.Success,
            StartTime = startTime,
            EndTime = endTime,
            ExitCode = 0
        };

    public static BuildStepResult Failed(string stepName, DateTime startTime, DateTime endTime, string errorMessage,
        string? stackTrace = null, int exitCode = 1)
        => new BuildStepResult
        {
            StepName = stepName,
            Status = StepStatus.Failed,
            StartTime = startTime,
            EndTime = endTime,
            ErrorMessage = errorMessage,
            StackTrace = stackTrace,
            ExitCode = exitCode
        };

    public static BuildStepResult Skipped(string stepName)
        => new BuildStepResult
        {
            StepName = stepName,
            Status = StepStatus.Skipped,
            StartTime = DateTime.MinValue,
            EndTime = DateTime.MinValue,
            ExitCode = 0
        };
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
    public required DateTime BuildStartTime { get; init; }
    public DateTime BuildEndTime { get; private set; }
    public List<BuildStepResult> StepResults { get; private set; } = new();

    public TimeSpan TotalDuration => BuildEndTime - BuildStartTime;
    public bool IsComplete => BuildEndTime != default;

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
    public void RecordSuccess(string stepName, DateTime startTime, DateTime endTime)
    {
        StepResults.RemoveAll(r => r.StepName == stepName);
        StepResults.Add(BuildStepResult.Success(stepName, startTime, endTime));
    }

    /// <summary>
    /// Records a failed step execution
    /// </summary>
    public void RecordFailure(string stepName, DateTime startTime, DateTime endTime, string errorMessage,
        string? stackTrace = null, int exitCode = 1)
    {
        StepResults.RemoveAll(r => r.StepName == stepName);
        StepResults.Add(BuildStepResult.Failed(stepName, startTime, endTime, errorMessage, stackTrace, exitCode));
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
    /// Marks the build as complete
    /// </summary>
    public void Complete()
    {
        BuildEndTime = DateTime.Now;
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
            ProjectDirectory = projectDirectory,
            BuildStartTime = DateTime.Now
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

    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true
    };

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
/// Utilities for BuildResult operations
/// </summary>
public static class BuildResultUtils
{
    /// <summary>
    /// Executes a function and records the result in BuildResult
    /// </summary>
    public static int ExecuteAndRecord(BuildResult buildResult, string stepName, Func<int> action)
    {
        var startTime = DateTime.Now;
        buildResult.StartStep(stepName);

        try
        {
            var exitCode = action();
            var endTime = DateTime.Now;

            if (exitCode == 0)
            {
                buildResult.RecordSuccess(stepName, startTime, endTime);
                Console.WriteLine(
                    $"[BuildResult] ✅ {stepName} completed successfully in {(endTime - startTime).TotalSeconds:F1}s");
            }
            else
            {
                buildResult.RecordFailure(stepName, startTime, endTime, $"Exit code: {exitCode}");
                Console.WriteLine($"[BuildResult] ❌ {stepName} failed with exit code {exitCode}");
            }

            return exitCode;
        }
        catch (Exception ex)
        {
            var endTime = DateTime.Now;
            buildResult.RecordFailure(stepName, startTime, endTime, ex.Message, ex.StackTrace);

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
