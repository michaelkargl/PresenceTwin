# Architecture Analysis: Weather Data API

## Current Setup Analysis

### Project Structure
```
PresenceTwin.Api/
├── Program.fs              # Application entry point, DI configuration
├── WeatherForecast.fs      # Domain model
├── Errors.fs              # Error types
└── Controllers/
    └── WeatherForecastController.fs  # HTTP handlers and OpenAPI specs
```

### Current Implementation Assessment

#### ✅ What's Working Well

1. **Type-Safe Domain Model**
   - `WeatherForecast` is a pure F# record type
   - Computed property `TemperatureF` is immutable
   - Clean separation in its own file

2. **Explicit Error Types**
   - `TestHttpError` is defined separately
   - Type-safe error handling setup

3. **Functional Pipeline in Program.fs**
   - Using function composition for app configuration
   - Private functions keep implementation details hidden
   - Clear separation of services and app configuration

#### ❌ Current Issues & Anti-Patterns

1. **Mixed Concerns in Controller**
   - Business logic (weather generation) is tightly coupled to HTTP handler
   - OpenAPI specification mixed with handler logic
   - Random data generation inside handler
   - Direct dependency on `HttpContext`

2. **Non-Functional Aspects**
   - `Task.Delay(1)` side effect in handler
   - Direct use of `Random.Shared` (impure)
   - DateTime.Now (impure, non-deterministic)
   - Handler returns `Task` instead of `Task<'T>`

3. **Missing Abstractions**
   - No domain service layer
   - No separation between HTTP concerns and business logic
   - Handler is both coordinator and implementer

4. **Incomplete Route Configuration**
   - `Program.fs` defines route but doesn't connect to handler
   - Missing the actual handler registration

## Best Practices for Functional F# Web APIs

### 1. Layered Architecture

```
┌─────────────────────────────────────┐
│        HTTP Layer (Oxpecker)        │  ← Thin, only HTTP concerns
├─────────────────────────────────────┤
│       Application Layer             │  ← Orchestration, composition
├─────────────────────────────────────┤
│        Domain Layer                 │  ← Pure business logic
├─────────────────────────────────────┤
│    Infrastructure Layer (optional)  │  ← External dependencies
└─────────────────────────────────────┘
```

### 2. Separation of Concerns

#### Domain Layer (Pure Functions)
- No dependencies on HTTP, frameworks, or I/O
- Pure functions that transform data
- All inputs as parameters (no hidden dependencies)
- Testable without mocking

#### Application Layer
- Composes domain functions
- Handles cross-cutting concerns
- Can have dependencies (injected as functions)

#### HTTP Layer
- Only HTTP concerns (routing, serialization, status codes)
- Minimal logic - delegates to application layer
- OpenAPI specs separate from handlers

### 3. Dependency Injection - Functional Style

Instead of injecting services, inject **functions**:
```fsharp
type WeatherService = 
    { GenerateForecasts: int -> DateTime -> WeatherForecast[] }

// Or even simpler - just a function type
type GenerateForecasts = int -> DateTime -> WeatherForecast[]
```

### 4. Pure vs Impure Separation

**Pure (Domain):**
- Deterministic transformations
- No side effects
- Easy to test

**Impure (Infrastructure/HTTP):**
- I/O operations
- Current time, random numbers
- Database calls
- HTTP requests

### 5. Error Handling

Use `Result<'T, 'Error>` for domain errors:
```fsharp
type WeatherError =
    | InvalidCount of int
    | DateRangeError of string

type GenerateForecasts = int -> DateTime -> Result<WeatherForecast[], WeatherError>
```

### 6. OpenAPI Specification Separation

Keep OpenAPI specs in separate modules or files - they're metadata, not logic.

## Recommended Refactoring

### File Structure
```
PresenceTwin.Api/
├── Program.fs
├── Domain/
│   ├── Models.fs           # Domain types (WeatherForecast)
│   └── Weather.fs          # Pure domain logic
├── Application/
│   └── WeatherService.fs   # Application services
├── Infrastructure/
│   └── RandomGenerator.fs  # Impure dependencies (optional)
└── Http/
    ├── Endpoints.fs        # Route definitions
    ├── Handlers/
    │   └── WeatherHandlers.fs
    └── OpenApi/
        └── WeatherOpenApi.fs
```

### Key Principles

1. **Domain First**: Start with pure domain models and functions
2. **Push I/O to Edges**: Keep side effects at boundaries
3. **Function Composition**: Build complex behavior from simple functions
4. **Explicit Dependencies**: All dependencies as function parameters
5. **Type-Driven Design**: Use types to make invalid states unrepresentable
6. **Test Pure Code**: 95% of logic should be pure and easily testable

### Benefits of This Approach

- **Testability**: Pure functions test without infrastructure
- **Maintainability**: Clear boundaries and responsibilities
- **Reusability**: Domain logic independent of HTTP
- **Flexibility**: Easy to swap HTTP frameworks
- **Reasoning**: Pure functions are easier to understand
- **Parallel Development**: Teams can work on layers independently

## Implementation Notes

### HttpContext Usage
Minimize direct `HttpContext` usage. Extract what you need:
```fsharp
// ❌ Bad: Handler takes HttpContext and does everything
let handler (ctx: HttpContext) = ...

// ✅ Good: Handler extracts data, calls pure function
let handler (generateForecasts: GenerateForecasts) (count: int) (ctx: HttpContext) =
    let forecasts = generateForecasts count DateTime.Now
    ctx.WriteJson forecasts
```

### Async Considerations
Use `task` for HTTP layer, but domain can be synchronous if logic is pure:
```fsharp
// Domain: Pure, synchronous
let generateForecasts count startDate = ...

// HTTP Layer: Async for I/O
let handler ... (ctx: HttpContext) = task {
    let forecasts = generateForecasts count DateTime.Now
    return! ctx.WriteJson forecasts
}
```

### Configuration
Use Reader pattern or partial application for configuration:
```fsharp
type WeatherConfig = { Summaries: string list; MinTemp: int; MaxTemp: int }

let generateForecasts (config: WeatherConfig) (count: int) (startDate: DateTime) =
    // Use config.Summaries, config.MinTemp, etc.
    ...

// In Program.fs - partial application
let config = { Summaries = [...]; MinTemp = -20; MaxTemp = 55 }
let generateForecasts' = generateForecasts config
```

## Next Steps

1. Create Domain layer with pure functions
2. Create Application layer for orchestration
3. Refactor HTTP handlers to be thin wrappers
4. Separate OpenAPI specifications
5. Add proper error handling with Result types
6. Write tests for pure domain logic
7. Consider adding validation layer

## Conclusion

The current implementation is a good starting point but mixes concerns. By separating:
- **Domain** (what we do) from **HTTP** (how we expose it)
- **Pure** logic from **impure** I/O
- **Business rules** from **infrastructure**

We achieve a more functional, maintainable, and testable codebase that scales better as complexity grows.

