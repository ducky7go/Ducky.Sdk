# Change: Enhance BuildResult Skip Status Tracking and Display Formatting

## Why
Currently, when script libraries are skipped they return exit code 0, which makes them indistinguishable from successful executions in the BuildResult tracking. Additionally, the PrintResultLib output includes unnecessary empty lines between steps, making the output verbose and harder to read.

## What Changes
- **BREAKING**: Script libraries will return exit code 36524 instead of 0 when they are intentionally skipped
- **MODIFIED**: BuildResultUtils.ExecuteAndRecord will recognize exit code 36524 as skip status
- **MODIFIED**: PrintResultLib will remove unnecessary empty lines from step display output
- **ADDED**: Constant definition for skip exit code to maintain consistency across the codebase

## Impact
- Affected specs: mod-build
- Affected code: entry.csx, BuildResult.cs, PrintResultLib.cs, and all script library files that may return skip status
- BuildResult tracking will now correctly distinguish between success and skip states
- Console output will be more compact and readable