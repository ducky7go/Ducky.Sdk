# Backward Compatibility Requirements and Migration Strategy

## Compatibility Commitment

This refactor maintains 100% backward compatibility with existing Ducky.Sdk projects. All existing MSBuild properties, targets, and behaviors are preserved.

## What Stays the Same

### Existing MSBuild Properties
- `ModName` - Unchanged behavior
- `DuckovFolder` - Unchanged behavior
- `SteamFolder` - Unchanged behavior
- `AssetsDir` - Unchanged behavior
- `LocalizationAssetsDir` - Unchanged behavior
- `EnableILRepack` - Unchanged behavior
- `EnableGlobalUsing` - Unchanged behavior
- `IncludeHarmony` - Unchanged behavior
- `DeployMod` - Unchanged behavior
- `IsModLib` - Unchanged behavior

### Existing MSBuild Targets
- All existing targets remain functional
- Target execution order is preserved
- All existing conditions and dependencies are maintained

### Existing Scripts
- `ensure-info-ini.csx` - Enhanced but backward compatible
- `generate-preview.csx` - Enhanced but backward compatible
- `update-locales-csv.csx` - Enhanced but backward compatible
- `extract-lkeys-json.csx` - Enhanced but backward compatible
- `collect-from-mod.csx` - Enhanced but backward compatible

## What's New (Opt-In)

### CSX-Enhanced Validation
- New CSX-based validation targets are automatically used when available
- Fallback to original XML-based validation if CSX scripts are missing
- Improved error messages and detailed diagnostics

### Centralized Property Resolution
- Enhanced `resolve-sdk-properties.csx` script for better property management
- Improved project type detection and configuration validation
- Better handling of edge cases and conflicting configurations

### Performance Optimizations
- Intelligent caching for expensive operations
- Conditional execution based on file changes
- Reduced redundant property calculations

## Migration Path

### For Existing Projects
**No action required** - existing projects will continue to work exactly as before.

### For New Projects
New projects automatically benefit from the enhanced CSX-based system without any configuration changes.

### For Advanced Users
Advanced users can opt-in to additional features by:
- Using the new CSX scripts directly for custom build logic
- Leveraging the `BuildContext` class for custom scripts
- Taking advantage of the enhanced caching system

## Fallback Mechanisms

The system includes multiple fallback layers to ensure compatibility:

1. **CSX Script Fallback**: If CSX scripts are not available, the system falls back to original XML-based targets
2. **Property Fallback**: If CSX property resolution fails, existing MSBuild property logic is used
3. **Validation Fallback**: If enhanced validation is not available, basic validation is performed

## Testing Strategy

### Compatibility Testing
- All existing sample projects continue to build successfully
- No breaking changes to public APIs or behaviors
- Performance improvements are transparent to users

### Regression Testing
- All existing MSBuild targets produce identical output
- Property calculations remain consistent
- Build process execution order is preserved

## Gradual Rollout

The new CSX-enhanced system is designed for gradual adoption:

1. **Phase 1**: CSX scripts coexist with existing targets
2. **Phase 2**: CSX scripts are automatically preferred when available
3. **Phase 3**: Legacy targets are deprecated but remain functional
4. **Phase 4**: Legacy targets may be removed in future major versions

## Configuration Options

### Disabling CSX Enhancements
Users can disable CSX enhancements by setting:
```xml
<PropertyGroup>
  <EnableCSXEnhancements>false</EnableCSXEnhancements>
</PropertyGroup>
```

### Verbose Logging
Enable detailed logging from CSX scripts:
```xml
<PropertyGroup>
  <EnableCSXVerboseLogging>true</EnableCSXVerboseLogging>
</PropertyGroup>
```

### Debug Mode
Enable debugging information for CSX scripts:
```xml
<PropertyGroup>
  <EnableCSXDebugMode>true</EnableCSXDebugMode>
</PropertyGroup>
```

## Performance Impact

### Existing Projects
- No performance regression
- Potential performance improvements from caching
- Faster property resolution in most cases

### Build Times
- Initial build: Same or slightly faster (CSX script compilation overhead)
- Incremental builds: Significantly faster due to improved caching
- Clean builds: Comparable performance with better error reporting

## Error Handling

### Enhanced Error Messages
CSX scripts provide more detailed error messages while maintaining compatibility with existing error handling.

### Graceful Degradation
If CSX enhancements fail, the system gracefully degrades to the original behavior without breaking the build.

## Documentation

### Migration Guide
- No migration required for existing projects
- Optional documentation for advanced users who want to leverage new features

### Breaking Changes
No breaking changes are introduced in this refactor. All changes are additive and opt-in.