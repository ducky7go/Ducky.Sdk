#!/usr/bin/env dotnet-script
#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// Displays build results with ASCII art, emojis, and colored output
/// </summary>
public static class PrintResultLib
{
    /// <summary>
    /// Displays build results with enhanced visual formatting
    /// </summary>
    public static int Execute(BuildContext context, BuildResult buildResult)
    {
        try
        {
            // Mark the build as complete since we're displaying results
            if (!buildResult.IsComplete)
            {
                buildResult.Complete();
            }

            Console.WriteLine();
            DisplayHeader();
            DisplayBuildSummary(buildResult);
            DisplayStepDetails(buildResult);
            DisplayFooter();

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine("[ERROR] ERROR displaying results");
            Console.WriteLine($"        {ex.Message}");
            return 1;
        }
    }

    private static void DisplayHeader()
    {
        var header = @"
    +--------------------------------------------------------------+
    |                    DUCKY BUILD RESULTS                       |
    +--------------------------------------------------------------+";

        Console.WriteLine(header);
        Console.WriteLine();
    }

    private static void DisplayBuildSummary(BuildResult buildResult)
    {
        var duration = buildResult.IsComplete
            ? $"{buildResult.TotalDuration.TotalSeconds:F1}s"
            : "ongoing";

        var overallStatus = buildResult.OverallSuccess ? "[SUCCESS]" : "[FAILED]";

        // Summary line with box
        var summaryLine = $"    [INFO] Build Status: {overallStatus} | Duration: {duration} | Steps: {buildResult.TotalSteps}";
        var boxWidth = summaryLine.Length - 4;

        Console.WriteLine("    " + new string('=', boxWidth));
        Console.WriteLine(summaryLine);
        Console.WriteLine("    " + new string('=', boxWidth));
        Console.WriteLine();

        // Step counts
        Console.WriteLine($"    [OK]   Successful: {buildResult.SuccessfulSteps}");
        Console.WriteLine($"    [FAIL] Failed: {buildResult.FailedSteps}");
        Console.WriteLine($"    [SKIP] Skipped: {buildResult.SkippedSteps}");
        Console.WriteLine();
    }

    private static void DisplayStepDetails(BuildResult buildResult)
    {
        if (!buildResult.StepResults.Any())
        {
            Console.WriteLine("    [INFO] No build steps recorded");
            return;
        }

        Console.WriteLine("    [DETAILS] STEP EXECUTION:");
        Console.WriteLine("    " + new string('-', 60));
        Console.WriteLine();

        foreach (var step in buildResult.StepResults.OrderBy(s => s.StartTime))
        {
            DisplaySingleStep(step);
            Console.WriteLine();
        }
    }

    private static void DisplaySingleStep(BuildStepResult step)
    {
        var statusTag = step.Status switch
        {
            StepStatus.Success => "[OK]",
            StepStatus.Failed => "[FAIL]",
            StepStatus.Skipped => "[SKIP]",
            _ => "[?]"
        };

        var stepName = TruncateWithEllipsis(step.StepName, 40);

        Console.Write($"    {statusTag} {stepName}");

        switch (step.Status)
        {
            case StepStatus.Success:
                var duration = step.Duration.TotalSeconds.ToString("F1");
                Console.WriteLine($" ({duration}s)");
                break;

            case StepStatus.Failed:
                Console.WriteLine();
                if (!string.IsNullOrEmpty(step.ErrorMessage))
                {
                    Console.WriteLine($"         Error: {step.ErrorMessage}");
                }
                if (!string.IsNullOrEmpty(step.StackTrace) && step.StackTrace.Length > 100)
                {
                    var shortTrace = step.StackTrace.Substring(0, 100) + "...";
                    Console.WriteLine($"         Stack: {shortTrace}");
                }
                break;

            case StepStatus.Skipped:
                Console.WriteLine(" (skipped)");
                break;
        }
    }

    private static void DisplayFooter()
    {
        var footer = @"

    [COMPLETE] Build completed! Check the results above for any issues.
    [INFO]     All build results are saved in obj/ducky-build-result.json

";
        Console.WriteLine(footer);
    }

    private static string TruncateWithEllipsis(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;

        return text.Substring(0, maxLength - 3) + "...";
    }
}