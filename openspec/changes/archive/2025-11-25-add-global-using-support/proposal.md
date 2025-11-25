# Change: Add global using support for SDK namespaces

## Why
Simplify mod development by automatically importing common SDK, game engine, and Unity namespaces when the Ducky.Sdk is referenced, reducing the need for manual using statements and improving developer productivity.

## What Changes
- Add global using directives to Ducky.Sdk.props for essential SDK namespaces
- Include global using for common game engine DLL namespaces (TeamSoda, Unity)
- Add global using for frequently used third-party libraries (DOTween, Newtonsoft.Json)
- Provide configuration option to disable automatic global usings for projects that prefer explicit control
- Maintain backward compatibility with existing projects

## Impact
- Affected specs: mod-build (build system enhancements)
- Affected code:
  - `Sdk/SDKlibs/Ducky.Sdk/Ducky.Sdk.props` - Add global using directives
  - Sample projects will benefit from reduced using statements