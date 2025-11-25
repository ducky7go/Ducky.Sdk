## 1. Implementation
- [x] 1.1 Add LocalizationAssetsDir property definition to Ducky.Sdk.props
- [x] 1.2 Modify UpdateLocalesCsv target to use LocalizationAssetsDir as primary source
- [x] 1.3 Add fallback logic to use AssetsDir when LocalizationAssetsDir is not specified
- [x] 1.4 Update target logging to show which property is being used
- [x] 1.5 Test the implementation with sample projects

## 2. Documentation
- [x] 2.1 Update README.md MSBuild properties table to include LocalizationAssetsDir
- [x] 2.2 Add documentation explaining the difference between AssetsDir and LocalizationAssetsDir
- [x] 2.3 Update localization system documentation with migration guidance
- [x] 2.4 Add examples showing usage of both properties

## 3. Validation
- [x] 3.1 Verify existing projects continue to work without changes
- [x] 3.2 Test new property with single directory path
- [x] 3.3 Test new property with multiple directory paths (semicolon-separated)
- [x] 3.4 Validate that other targets are not affected by this change
- [x] 3.5 Run all sample projects to ensure compatibility

## Additional Implementation
- [x] Fixed LKeysJsonPath to use LocalizationAssetsDir instead of AssetsDir for consistency