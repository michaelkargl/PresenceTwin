# Functional Architecture Analysis - Weather API

## Current Implementation Analysis

### ✅ What's Working Well

1. **Functional Style**
   - No OOP classes, only Record Types and DUs
   - Pure domain logic separated from I/O
   - Function composition with Result types
   - Dependency injection via partial application

2. **CQRS Pattern**
   - Clear separation between Commands (GenerateForecasts) and Queries (GetForecast)
   - Each operation is self-contained
   - Command returns CommandResult, Query returns QueryResult

3. **Railway-Oriented Programming**
   - Result types for error handling
   - Domain errors (InvalidCount, InvalidStartDate)
   - Error mapping to HTTP responses

4. **Separation of Concerns**
   - Domain.fs: Pure domain models and logic
   - GetForecast/GenerateForecasts: Business orchestration
   - Endpoints.fs: HTTP routing and dependency wiring
   - Http.fs: Reusable HTTP utilities

5. **Dependency Injection**
   - Abstract interfaces (IRandomProvider, ITimeProvider)
   - Dependencies record per operation
   - Partial application in Endpoints.fs
   - Easy to mock for testing

6. **Pure vs Impure Separation**
   - Pure: `Domain.generateForecasts`, `validate` functions
   - Impure: `execute` functions (use TimeProvider, RandomProvider)
   - Impure dependencies injected from outside

### ⚠️ Areas for Improvement

#### 1. **Namespace Pattern** (Critical)
**Issue**: Multiple modules in one namespace
```fsharp
namespace PresenceTwin.Api.Features.Weather

module GetForecast = ...
module GenerateForecasts = ...
module Domain = ...
module Endpoints = ...
```

**Should Be**: One namespace per file matching file path
```fsharp
// File: Features/Weather/Domain.fs
namespace PresenceTwin.Api.Features.Weather.Domain

// File: Features/Weather/GetForecast.fs  
namespace PresenceTwin.Api.Features.Weather.GetForecast

// File: Features/Weather/GenerateForecasts.fs
namespace PresenceTwin.Api.Features.Weather.GenerateForecasts

// File: Features/Weather/Endpoints.fs
namespace PresenceTwin.Api.Features.Weather.Endpoints
```

#### 2. **Type Extensions on Records**
**Issue**: `WeatherForecast` has a computed member
```fsharp
type WeatherForecast = {
    Date: DateTime
    TemperatureC: int
    Summary: string
}
with
    member this.TemperatureF = ...  // ❌ Type extension, OOP-style
```

**Should Be**: Module function instead
```fsharp
type WeatherForecast = {
    Date: DateTime
    TemperatureC: int
    Summary: string
}

// Separate module with functions
module WeatherForecast =
    let temperatureF (forecast: WeatherForecast) : float =
        32.0 + (float forecast.TemperatureC / 0.5556)
```

#### 3. **TimeProvider Interface**
**Issue**: Mixed abstraction levels
```fsharp
type ITimeProvider = { ... }  // Record-based interface
let createTimeProvider () : unit -> DateTime  // Function-based
```

**Should Be**: Consistent function-based approach
```fsharp
type GetCurrentTime = unit -> DateTime
type GetRandomInt = int -> int -> int

type Dependencies = {
    Config: WeatherConfig
    GetCurrentTime: GetCurrentTime
    GetRandomInt: GetRandomInt
}
```

#### 4. **File Ordering in .fsproj**
Must maintain bottom-up dependency order:
```xml
<ItemGroup>
  <!-- 1. Pure Domain (no dependencies) -->
  <Compile Include="Features/Weather/Domain.fs" />
  
  <!-- 2. Operations (depend on Domain) -->
  <Compile Include="Features/Weather/GetForecast.fs" />
  <Compile Include="Features/Weather/GenerateForecasts.fs" />
  
  <!-- 3. Endpoints (depend on Operations) -->
  <Compile Include="Features/Weather/Endpoints.fs" />
</ItemGroup>
```

## Refactoring Plan

### Phase 1: Namespace Restructure ✅
- [x] One namespace per file
- [x] Namespace matches file path
- [x] Update imports across files

### Phase 2: Remove Type Extensions ✅
- [x] Convert `WeatherForecast.TemperatureF` member to module function
- [x] Create `WeatherForecast` module with helper functions

### Phase 3: Simplify Dependencies ✅
- [x] Use type aliases for function signatures
- [x] Remove interface records, use functions directly
- [x] Consistent dependency pattern

### Phase 4: Validation ✅
- [x] Ensure .fsproj file ordering is correct
- [x] Build and test
- [x] Verify no compilation errors

## Best Practices Summary

### 1. **Pure Functional Core**
```fsharp
// ✅ GOOD: Pure function
let calculateTemperatureF (tempC: int) : float =
    32.0 + (float tempC / 0.5556)

// ❌ BAD: Type extension
type Forecast with
    member this.TemperatureF = ...
```

### 2. **Dependency Injection Pattern**
```fsharp
// Type aliases for clarity
type GetCurrentTime = unit -> DateTime
type GetRandomInt = int -> int -> int

// Dependencies as record of functions
type Dependencies = {
    GetCurrentTime: GetCurrentTime
    GetRandomInt: GetRandomInt
    Config: WeatherConfig
}

// Partial application in composition root
let queryDeps = {
    GetCurrentTime = fun () -> DateTime.UtcNow
    GetRandomInt = fun min max -> Random.Shared.Next(min, max)
    Config = weatherConfig
}

// Handler uses dependencies
let handle (deps: Dependencies) (query: Query) : Result<QueryResult, QueryError> =
    // Implementation
```

### 3. **Pipeline Composition**
```fsharp
// ✅ GOOD: Flat pipeline
let handle deps query =
    query
    |> validate deps.Config
    |> Result.map (execute deps)

// ❌ BAD: Nested callbacks
let handle deps query =
    match validate deps.Config query with
    | Ok validQuery ->
        match execute deps validQuery with
        | Ok result -> Ok result
        | Error e -> Error e
    | Error e -> Error e
```

### 4. **Pure vs Impure Separation**
```fsharp
// Pure (no side effects, always same output for same input)
let validate (config: Config) (query: Query) : Result<Query, Error> = ...
let calculateTemperature (tempC: int) : float = ...

// Impure (has side effects or non-deterministic)
let execute (deps: Dependencies) (query: Query) : QueryResult =
    let now = deps.GetCurrentTime()  // Impure: current time
    let random = deps.GetRandomInt 1 10  // Impure: randomness
    // Use pure domain functions with impure inputs
    Domain.generateForecasts config.Summaries [...] now count
```

### 5. **Error Handling**
```fsharp
// Domain errors (expected)
type QueryError =
    | InvalidCount of int * string
    | InvalidDate of string

// Map to HTTP responses
let mapErrorToHttp (error: QueryError) (ctx: HttpContext) : Task =
    match error with
    | InvalidCount (count, msg) ->
        Http.writeBadRequest "InvalidCount" msg (Some {| count = count |}) ctx
    | InvalidDate msg ->
        Http.writeBadRequest "InvalidDate" msg None ctx
```

### 6. **Module Organization Within File**
```fsharp
namespace PresenceTwin.Api.Features.Weather.GetForecast

// 1. Types (top, pure data)
type Query = { Count: int }
type QueryResult = Domain.WeatherForecast array
type QueryError = | InvalidCount of int * string

// 2. Dependencies (pure data)
type Dependencies = { ... }

// 3. Private validation (pure functions)
let private validate config query = ...

// 4. Private execution (impure orchestration)
let private execute deps query = ...

// 5. Public handler (composition)
let handle deps query = ...

// 6. HTTP layer (impure I/O)
let httpHandler deps count ctx = ...

// 7. OpenAPI spec (configuration)
let configureOpenApi endpoint = ...
```

## Vertical Slice Structure

```
Features/Weather/
├── Domain.fs                 # Pure domain types & functions
├── GetForecast.fs           # Query: Read operation
├── GenerateForecasts.fs     # Command: Write operation
└── Endpoints.fs             # HTTP routing & DI composition root
```

### Domain.fs
- **Purpose**: Pure domain models and business logic
- **Dependencies**: None (System only)
- **Contains**: Types, pure functions, business rules
- **No I/O**: No database, HTTP, randomness, time

### GetForecast.fs (Query)
- **Purpose**: Read operation
- **Dependencies**: Domain
- **Contains**: Query model, validation, handler, HTTP endpoint
- **CQRS**: Query - does not mutate state

### GenerateForecasts.fs (Command)
- **Purpose**: Write operation
- **Dependencies**: Domain
- **Contains**: Command model, validation, handler, HTTP endpoint
- **CQRS**: Command - can mutate state (currently generates data)

### Endpoints.fs
- **Purpose**: Composition root, wire up HTTP routes
- **Dependencies**: All operations
- **Contains**: Dependency injection, route configuration
- **Role**: Top-level orchestration

## Testing Strategy

### Unit Tests (Pure Functions)
```fsharp
// Test pure domain logic
let ``generateForecasts should create correct number of forecasts`` () =
    let summaries = ["Cold"; "Warm"]
    let temps = [| 10; 20; 30 |]
    let indices = [| 0; 1; 0 |]
    let startDate = DateTime(2025, 1, 1)
    
    let result = Domain.generateForecasts summaries temps indices startDate 3
    
    result.Length |> should equal 3
    result.[0].TemperatureC |> should equal 10
```

### Integration Tests (HTTP Endpoints)
```fsharp
// Test full HTTP pipeline with mocked dependencies
let ``GET /api/weather/forecast/5 returns 5 forecasts`` () = task {
    let testDeps = {
        Config = testConfig
        GetCurrentTime = fun () -> DateTime(2025, 1, 1)
        GetRandomInt = fun _ _ -> 15  // Predictable
    }
    
    let! response = client.GetAsync("/api/weather/forecast/5")
    
    response.StatusCode |> should equal HttpStatusCode.OK
    let! forecasts = response.Content.ReadFromJsonAsync<WeatherForecast[]>()
    forecasts.Length |> should equal 5
}
```

## Performance Considerations

1. **Immutability**: F# records are efficient, no defensive copying needed
2. **Tail Recursion**: Use `List.fold` or `Array.map` over manual recursion
3. **Lazy Evaluation**: Use `seq` for large datasets
4. **Async**: Use `task { }` CE for async I/O operations

## Common Pitfalls to Avoid

1. ❌ **Mixing OOP and FP**: No classes, inheritance, or type extensions
2. ❌ **Deep Nesting**: Keep orchestration flat, extract functions
3. ❌ **Impure in Pure**: Don't call DateTime.Now or Random in pure functions
4. ❌ **Generic Result**: Use specific error types per operation
5. ❌ **Exception for Control Flow**: Use Result for expected errors
6. ❌ **Multiple Namespaces per File**: One namespace per file only

## Next Steps

When adding a new feature:

1. **Create Folder**: `Features/NewFeature/`
2. **Add Domain**: `Domain.fs` with pure types and functions
3. **Add Operations**: 
   - `GetSomething.fs` for queries
   - `CreateSomething.fs` for commands
4. **Add Endpoints**: `Endpoints.fs` for HTTP routing
5. **Update .fsproj**: Add files in bottom-up order
6. **Wire in Program.fs**: Register endpoints

## Conclusion

This architecture achieves:
- ✅ 100% functional style (no OOP)
- ✅ Clear separation of pure and impure code
- ✅ Easy testing and mocking
- ✅ CQRS pattern with vertical slices
- ✅ Railway-oriented programming with Result types
- ✅ Dependency injection via partial application
- ✅ Oxpecker framework kept at the edges

The refactoring to one-namespace-per-file will improve:
- Code navigation
- Explicit dependencies
- Module isolation
- Maintainability

