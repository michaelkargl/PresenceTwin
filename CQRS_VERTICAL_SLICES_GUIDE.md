# CQRS with Vertical Slices Architecture

## Overview

**CQRS (Command Query Responsibility Segregation)** separates read operations (Queries) from write operations (Commands). Combined with **Vertical Slices**, each feature is self-contained in its own folder with all its layers.

## Why CQRS + Vertical Slices?

### Benefits
- ✅ **Feature cohesion**: Everything for one feature in one place
- ✅ **Independent scaling**: Optimize reads and writes separately
- ✅ **Parallel development**: Teams work on different slices
- ✅ **Clear boundaries**: Commands change state, queries don't
- ✅ **Easy to find**: Feature-based navigation
- ✅ **Functional fit**: Natural for F# with immutability and Result types

### CQRS Principles
1. **Commands**: Mutate state, return success/failure
2. **Queries**: Read data, never mutate, can be cached
3. **Separation**: Different models for reading and writing (optional)

## Folder Structure

```
PresenceTwin.Api/
├── Program.fs
├── Common/                          # Shared utilities
│   ├── Result.fs                   # Result helpers
│   ├── Validation.fs               # Common validations
│   └── Http.fs                     # HTTP utilities
├── Infrastructure/                  # Cross-cutting concerns
│   ├── Database/
│   ├── Logging/
│   └── Configuration/
└── Features/                        # Vertical slices
    └── Weather/                     # One feature
        ├── Domain.fs               # Domain models & pure logic
        ├── Commands/               # Write operations
        │   ├── CreateForecast.fs
        │   └── UpdateForecast.fs
        ├── Queries/                # Read operations
        │   ├── GetForecast.fs
        │   └── ListForecasts.fs
        └── Endpoints.fs            # HTTP endpoints for this feature
```

## File Naming Convention

Each command/query file contains everything for that operation:
- Domain types
- Validation
- Handler
- HTTP endpoint
- OpenAPI spec

## CQRS Pattern Structure

### Command Structure
```fsharp
module PresenceTwin.Api.Features.Weather.Commands.CreateForecast

// 1. Command (input)
type CreateForecastCommand = { ... }

// 2. Result (output)
type CreateForecastResult = { ... }

// 3. Error types
type CreateForecastError = ...

// 4. Domain logic (pure)
let validate command = ...
let execute command = ...

// 5. Handler (orchestration)
let handle dependencies command = ...

// 6. HTTP endpoint
let endpoint = ...

// 7. OpenAPI spec
let configureOpenApi = ...
```

### Query Structure
```fsharp
module PresenceTwin.Api.Features.Weather.Queries.GetForecast

// 1. Query (input)
type GetForecastQuery = { ... }

// 2. Result (output)  
type GetForecastResult = { ... }

// 3. Error types
type GetForecastError = ...

// 4. Domain logic (pure)
let validate query = ...
let fetch query = ...

// 5. Handler (orchestration)
let handle dependencies query = ...

// 6. HTTP endpoint
let endpoint = ...

// 7. OpenAPI spec
let configureOpenApi = ...
```

## Key Differences: Command vs Query

### Commands
- **Purpose**: Change state
- **HTTP Methods**: POST, PUT, PATCH, DELETE
- **Return**: Success/failure, possibly created resource
- **Side Effects**: Yes
- **Idempotent**: Depends on operation
- **Cacheable**: No

### Queries
- **Purpose**: Read data
- **HTTP Methods**: GET
- **Return**: Data
- **Side Effects**: No
- **Idempotent**: Yes
- **Cacheable**: Yes

## Dependency Injection Pattern

### Function-Based DI
```fsharp
type WeatherDependencies = {
    // Queries
    GetCurrentTime: unit -> DateTime
    ReadForecasts: int -> WeatherForecast[]
    
    // Commands  
    SaveForecast: WeatherForecast -> Result<unit, DbError>
    GenerateRandom: int -> int -> int
}
```

### Partial Application in Program.fs
```fsharp
let deps = createDependencies config

// Partially apply to create handlers
let getForecastHandler = GetForecast.handle deps
let createForecastHandler = CreateForecast.handle deps
```

## Example: Complete Feature

### Weather Domain Models
```fsharp
// Features/Weather/Domain.fs
module PresenceTwin.Api.Features.Weather.Domain

open System

type WeatherForecast = {
    Id: Guid
    Date: DateTime
    TemperatureC: int
    Summary: string
}

type WeatherSummary = 
    | Freezing | Bracing | Chilly | Cool | Mild
    | Warm | Balmy | Hot | Sweltering | Scorching

let summaryToString = function
    | Freezing -> "Freezing"
    | Bracing -> "Bracing"
    // ... etc

type WeatherConfig = {
    Summaries: WeatherSummary list
    MinTemp: int
    MaxTemp: int
}
```

## Common Module Pattern

### Result Helpers
```fsharp
// Common/Result.fs
module PresenceTwin.Api.Common.Result

let map f result =
    match result with
    | Ok v -> Ok (f v)
    | Error e -> Error e

let bind f result =
    match result with
    | Ok v -> f v
    | Error e -> Error e

let mapError f result =
    match result with
    | Ok v -> Ok v
    | Error e -> Error (f e)
```

### HTTP Helpers
```fsharp
// Common/Http.fs
module PresenceTwin.Api.Common.Http

open System.Threading.Tasks
open Microsoft.AspNetCore.Http

let writeJsonOk<'T> (data: 'T) (ctx: HttpContext) : Task =
    ctx.WriteJson data

let writeBadRequest (error: obj) (ctx: HttpContext) : Task =
    ctx.SetStatusCode 400
    ctx.WriteJson error

let writeNotFound (message: string) (ctx: HttpContext) : Task =
    ctx.SetStatusCode 404
    ctx.WriteJson {| error = "NotFound"; message = message |}
```

## Testing Strategy

### Domain Tests (Pure Functions)
```fsharp
module Weather.Domain.Tests

[<Test>]
let ``validation rejects invalid input`` () =
    let command = { Count = -1 }
    let result = validate command
    Assert.IsTrue(Result.isError result)
```

### Handler Tests (With Mock Dependencies)
```fsharp
module Weather.Commands.CreateForecast.Tests

[<Test>]
let ``handle creates forecast successfully`` () =
    let deps = {
        GetCurrentTime = fun () -> DateTime(2025, 1, 1)
        SaveForecast = fun _ -> Ok ()
        GenerateRandom = fun _ _ -> 10
    }
    
    let command = { Count = 5 }
    let result = handle deps command
    Assert.IsTrue(Result.isOk result)
```

### Integration Tests (HTTP Layer)
```fsharp
[<Test>]
let ``GET /weather/forecast returns 200`` () =
    // Test with TestServer
```

## Migration from Layered to Vertical Slices

### Step 1: Create Feature Folder
```
Features/Weather/
```

### Step 2: Move Domain
```
Domain/ → Features/Weather/Domain.fs
```

### Step 3: Split by Operation
```
Application/WeatherService.fs → 
    Features/Weather/Commands/GenerateForecast.fs
    Features/Weather/Queries/GetForecast.fs
```

### Step 4: Co-locate HTTP
```
Http/Handlers/WeatherHandlers.fs → 
    Features/Weather/Commands/GenerateForecast.fs (inline)
    Features/Weather/Queries/GetForecast.fs (inline)
```

### Step 5: Update Program.fs
```fsharp
// Collect all endpoints from features
let endpoints = [
    yield! Weather.Endpoints.endpoints deps
    yield! OtherFeature.Endpoints.endpoints deps
]
```

## Best Practices

### 1. Keep Commands Simple
Each command does ONE thing:
- ❌ `UpdateWeatherAndNotifyUsers`
- ✅ `UpdateWeather` + `NotifyUsers`

### 2. Queries Don't Change State
```fsharp
// ❌ Bad: Query with side effect
let getForecasts () =
    logger.Log("Fetching forecasts") // Side effect!
    fetchForecasts()

// ✅ Good: Pure query
let getForecasts () = fetchForecasts()
```

### 3. Use Specific Types
```fsharp
// ❌ Generic
type Command = { Data: Map<string, obj> }

// ✅ Specific
type CreateForecastCommand = {
    Date: DateTime
    TemperatureC: int
    Summary: string
}
```

### 4. Validate Early
```fsharp
let handle deps command =
    command
    |> validate        // Stop early if invalid
    |> Result.bind (execute deps)
```

### 5. Separate Read/Write Models (Optional)
```fsharp
// Write model - full details
type WeatherForecastWrite = {
    Id: Guid
    Date: DateTime
    TemperatureC: int
    TemperatureF: float
    Summary: string
    CreatedBy: string
    CreatedAt: DateTime
}

// Read model - only what UI needs
type WeatherForecastRead = {
    Date: DateTime
    Temperature: int
    Summary: string
}
```

## When to Use CQRS

### Good Fit ✅
- Different read/write performance requirements
- Complex domain with many operations
- Multiple teams working on same codebase
- Need to scale reads/writes independently
- Clear command/query separation in business logic

### Overkill ❌
- Simple CRUD operations
- Small applications
- No performance differences between reads/writes
- Single developer/small team
- Rapid prototyping phase

## Advanced Patterns

### Event Sourcing (Optional)
Commands produce events:
```fsharp
type WeatherEvent =
    | ForecastCreated of WeatherForecast
    | ForecastUpdated of Guid * WeatherForecast
    | ForecastDeleted of Guid

let handle command =
    command
    |> validate
    |> Result.map execute
    |> Result.map (fun forecast -> ForecastCreated forecast)
```

### Query Caching
```fsharp
let handle (cache: ICache) query =
    match cache.TryGet(query) with
    | Some result -> Ok result
    | None ->
        let result = fetchData query
        cache.Set(query, result)
        Ok result
```

### Command Validation Pipeline
```fsharp
let validate command =
    command
    |> validateCount
    |> Result.bind validateDateRange
    |> Result.bind validateSummary
    |> Result.bind validateTemperature
```

## Summary

**Vertical Slices + CQRS** gives you:
1. Feature-focused organization
2. Clear read/write separation
3. Independent scalability
4. Better team collaboration
5. Natural fit for functional programming

**Each slice is:**
- Self-contained
- Independently testable
- Loosely coupled
- Highly cohesive

