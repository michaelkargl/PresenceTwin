# F# Namespace Pattern: One Namespace Per File

## The Issue

In F#, **namespaces cannot directly contain values (functions, let bindings)**. They can only contain:
- Types (records, DUs, classes)
- Modules
- Other namespaces

## Solution: Add Module Inside Namespace

### Pattern for Each File

```fsharp
// File: Features/Weather/GetForecast.fs
namespace PresenceTwin.Api.Features.Weather.GetForecast

// All types can go directly in namespace
type Query = { Count: int }
type QueryResult = WeatherForecast array

// But functions MUST be in a module
[<AutoOpen>]  // This makes the module's contents available without qualification
module Operations =
    
    let validate query = ...
    let execute deps query = ...
    let handle deps query = ...
    let httpHandler deps count ctx = ...
```

### Why [<AutoOpen>]?

With `[<AutoOpen>]`, you can use:
```fsharp
GetForecast.handle deps query  // Works!
```

Without it, you'd need:
```fsharp
GetForecast.Operations.handle deps query  // Verbose
```

## Alternative: Namespace Matching Directory Only

This is MORE idiomatic F#:

```
PresenceTwin.Api/Features/Weather/GetForecast.fs
=> namespace PresenceTwin.Api.Features.Weather
=> module GetForecast =
```

**Benefits:**
1. More F# idiomatic
2. No need for [<AutoOpen>]
3. Cleaner references
4. Matches most F# codebases

## Recommendation

Given your requirement for enterprise F# patterns, I recommend:

### Files in `Features/Weather/`:
- `Domain.fs` → `namespace PresenceTwin.Api.Features.Weather` + `module Domain`
- `GetForecast.fs` → `namespace PresenceTwin.Api.Features.Weather` + `module GetForecast`
- `GenerateForecasts.fs` → `namespace PresenceTwin.Api.Features.Weather` + `module GenerateForecasts`
- `Endpoints.fs` → `namespace PresenceTwin.Api.Features.Weather` + `module Endpoints`

This way:
- Namespace reflects the **feature** (Weather)
- Module reflects the **operation** (GetForecast)
- Path: `Features/Weather/GetForecast.fs`
- Usage: `GetForecast.handle deps query`

This is the **standard enterprise F# pattern** used by:
- Microsoft F# team
- Jet.com
- G-Research
- Other major F# shops

Would you like me to implement this standard pattern, or do you have a specific reason for wanting the deeper namespace hierarchy?

