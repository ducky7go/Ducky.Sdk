# Ducky.Sdk

## Local Packaging

Use the helper script to build and pack the SDK into the local feed `./duckylocal` declared in `nuget.config`.

Steps:
1. Ensure you have the .NET SDK installed.
2. Run the packaging script:

```bash
./scripts/packToLocal.sh --version 1.0.0
```

Options:
- `--version x.y.z` Override the version in the nuspec.
- `--no-build` Skip building projects before packing (expects prior build artifacts).
- `--skip-tests` Skip test execution.
- `--configuration Debug|Release` Choose build configuration (default Debug).

After packing, install the package from the local feed in another project:

```bash
dotnet add package Ducky.Sdk --version 1.0.0 --source ./duckylocal
```

Update the version as needed if you packed a different one.
