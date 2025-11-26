# Change: Add Debug Logging to Build System

## Why
The current Ducky.Sdk build system lacks detailed logging output, making it difficult to diagnose build failures, understand target execution flow, or verify that CSX scripts are running correctly. When builds fail, developers get minimal visibility into where the process broke down.

## What Changes
- Add comprehensive debug logging to all MSBuild targets in `Ducky.Sdk.Orchestration.targets`
- Enhance `rebuild_samples.sh` script with better progress tracking and error reporting
- Add configurable verbosity levels to control log detail
- Improve CSX script execution logging with parameter visibility
- Add timing information for performance analysis

## Impact
- Affected specs: `mod-build`
- Affected code:
  - `Sdk/SDKlibs/Ducky.Sdk/Ducky.Sdk.Orchestration.targets`
  - `scripts/rebuild_samples.sh`
  - Various CSX scripts (minor logging enhancements)
- **BREAKING**: None - only adds logging, no behavior changes