# Refactoring Example: Weather Data API

This document shows a concrete refactoring of the current Weather API following functional best practices.

## Before: Current Implementation

**Issues:**
- Business logic mixed with HTTP handler
- Impure operations (Random, DateTime.Now) inside handler
- OpenAPI spec coupled to handler
- Not testable without HTTP context
- Handler doing too much

## After: Functional Architecture

### Step 1: Domain Layer (Pure)

#### `Domain/Models.fs`
```fsharp
namespace PresenceTwin.Api.Domain

open System

/// Domain model - immutable record
type WeatherForecast =
    { Date: DateTime
      TemperatureC: int
      Summary: string }
    
    member this.TemperatureF =
        32.0 + (float this.TemperatureC / 0.5556)

/// Domain-specific errors
type WeatherError =
    | InvalidForecastCount of int * string
    | InvalidDateRange of DateTime * DateTime

/// Validation result
type ValidationResult<'T> = Result<'T, WeatherError>
```

#### `Domain/Weather.fs`
```fsharp
module PresenceTwin.Api.Domain.Weather

open System
open PresenceTwin.Api.Domain

/// Pure function - generates forecast given all inputs
let createForecast (date: DateTime) (temperature: int) (summary: string) : WeatherForecast =
    { Date = date
      TemperatureC = temperature
      Summary = summary }

/// Pure function - validates count
let validateCount (count: int) : ValidationResult<int> =
    if count < 1 then
        Error (InvalidForecastCount (count, "Count must be at least 1"))
    elif count > 100 then
        Error (InvalidForecastCount (count, "Count cannot exceed 100"))
    else
        Ok count

/// Pure function - generates forecasts from provided random values
/// Takes all "impure" inputs as parameters
let generateForecasts 
    (summaries: string list)
    (randomTemperatures: int seq)
    (randomSummaryIndices: int seq)
    (startDate: DateTime)
    (count: int) 
    : WeatherForecast[] =
    
    let temps = randomTemperatures |> Seq.take count |> Seq.toArray
    let indices = randomSummaryIndices |> Seq.take count |> Seq.toArray
    
    [| for index in 0 .. count - 1 do
        createForecast
            (startDate.AddDays(float (index + 1)))
            temps.[index]
            summaries.[indices.[index]] |]

/// Pure function - calculates date range
let calculateDateRange (startDate: DateTime) (count: int) : DateTime * DateTime =
    let endDate = startDate.AddDays(float count)
    (startDate, endDate)
```

### Step 2: Application Layer (Orchestration)

#### `Application/WeatherService.fs`
```fsharp
module PresenceTwin.Api.Application.WeatherService

open System
open PresenceTwin.Api.Domain
open PresenceTwin.Api.Domain.Weather

/// Configuration - read from appsettings
type WeatherConfig =
    { Summaries: string list
      MinTemperature: int
      MaxTemperature: int }

/// Dependencies needed by the service
type WeatherDependencies =
    { GetCurrentTime: unit -> DateTime
      GetRandomInt: int -> int -> int
      Config: WeatherConfig }

/// Application service - coordinates pure domain logic with impure I/O
let generateWeatherForecasts 
    (deps: WeatherDependencies) 
    (count: int) 
    : Result<WeatherForecast[], WeatherError> =
    
    // Validate using pure function
    match validateCount count with
    | Error e -> Error e
    | Ok validCount ->
        // Get current time (impure, injected)
        let startDate = deps.GetCurrentTime()
        
        // Generate random sequences (impure, injected)
        let temperatures = 
            Seq.initInfinite (fun _ -> 
                deps.GetRandomInt deps.Config.MinTemperature deps.Config.MaxTemperature)
        
        let summaryIndices = 
            Seq.initInfinite (fun _ -> 
                deps.GetRandomInt 0 (deps.Config.Summaries.Length))
        
        // Call pure domain function with all inputs
        let forecasts = 
            generateForecasts 
                deps.Config.Summaries
                temperatures 
                summaryIndices 
                startDate 
                validCount
        
        Ok forecasts

/// Helper to create default dependencies
let createDefaultDependencies (config: WeatherConfig) : WeatherDependencies =
    { GetCurrentTime = fun () -> DateTime.Now
      GetRandomInt = fun min max -> Random.Shared.Next(min, max)
      Config = config }
```

### Step 3: HTTP Layer (Thin)

#### `Http/Handlers/WeatherHandlers.fs`
```fsharp
module PresenceTwin.Api.Http.Handlers.WeatherHandlers

open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Oxpecker
open PresenceTwin.Api.Domain
open PresenceTwin.Api.Application.WeatherService

/// Type alias for the weather generation function
type GenerateWeatherForecasts = int -> Result<WeatherForecast[], WeatherError>

/// Maps domain errors to HTTP responses
let private mapErrorToResponse (error: WeatherError) (ctx: HttpContext) : Task =
    match error with
    | InvalidForecastCount (count, msg) ->
        ctx.SetStatusCode(400)
        ctx.WriteJson {| error = "InvalidCount"; message = msg; count = count |}
    | InvalidDateRange (start, end') ->
        ctx.SetStatusCode(400)
        ctx.WriteJson {| error = "InvalidDateRange"; start = start; end' = end' |}

/// HTTP handler - thin wrapper that delegates to application service
let getWeatherForecastHandler 
    (generateForecasts: GenerateWeatherForecasts)
    (count: int) 
    (ctx: HttpContext) 
    : Task =
    task {
        match generateForecasts count with
        | Ok forecasts ->
            return! ctx.WriteJson forecasts
        | Error error ->
            return! mapErrorToResponse error ctx
    }
```

#### `Http/OpenApi/WeatherOpenApi.fs`
```fsharp
module PresenceTwin.Api.Http.OpenApi.WeatherOpenApi

open System.Collections.Generic
open Microsoft.OpenApi.Models
open Oxpecker
open Oxpecker.OpenApi
open PresenceTwin.Api.Domain

/// Configures OpenAPI specification for weather endpoint
let configureWeatherForecastEndpoint (endpoint: Endpoint) : Endpoint =
    endpoint
    |> addOpenApi (
        OpenApiConfig(
            responseBodies = [|
                ResponseBody(typeof<WeatherForecast[]>, statusCode = 200)
                ResponseBody(typeof<{| error: string; message: string |}>, statusCode = 400)
                ResponseBody(typeof<{| error: string; message: string |}>, statusCode = 500)
            |],
            configureOperation = fun o ->
                o.Summary <- "Get weather forecast"
                o.Description <- "Retrieve a list of weather forecasts for the specified number of days"
                o.OperationId <- "GetWeatherForecast"
                o.Tags <- List<OpenApiTag>([ OpenApiTag(Name = "Weather") ])
                
                o.Parameters <- List<OpenApiParameter>([
                    OpenApiParameter(
                        Name = "count",
                        In = ParameterLocation.Path,
                        Required = true,
                        Description = "Number of forecast days (1-100)",
                        Schema = OpenApiSchema(
                            Type = "integer",
                            Format = "int32",
                            Minimum = Nullable(1.0),
                            Maximum = Nullable(100.0)
                        )
                    )
                ])
                o
        )
    )
```

#### `Http/Endpoints.fs`
```fsharp
module PresenceTwin.Api.Http.Endpoints

open Oxpecker
open PresenceTwin.Api.Http.Handlers.WeatherHandlers
open PresenceTwin.Api.Http.OpenApi.WeatherOpenApi
open PresenceTwin.Api.Application.WeatherService

/// Creates endpoints with dependencies injected
let createEndpoints (generateForecasts: GenerateWeatherForecasts) : Endpoint list =
    [
        GET [
            routef "/weatherforecast/%i" (fun count ->
                getWeatherForecastHandler generateForecasts count
                |> configureWeatherForecastEndpoint
            )
        ]
    ]
```

### Step 4: Updated Program.fs

#### `Program.fs`
```fsharp
namespace PresenceTwin.Api

#nowarn "20"

open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Oxpecker
open Oxpecker.OpenApi
open PresenceTwin.Api.Application.WeatherService
open PresenceTwin.Api.Http.Endpoints

type ExitCode =
    | Success = 0
    | GeneralError = 1

module Program =
    /// Load configuration from appsettings
    let private loadWeatherConfig (config: IConfiguration) : WeatherConfig =
        { Summaries = 
            [ "Freezing"; "Bracing"; "Chilly"; "Cool"; "Mild"
              "Warm"; "Balmy"; "Hot"; "Sweltering"; "Scorching" ]
          MinTemperature = -20
          MaxTemperature = 55 }
    
    /// Configure application services
    let private configureServices (services: IServiceCollection) : unit =
        services
            .AddRouting()
            .AddAntiforgery()
            .AddOxpecker()
            .AddOpenApi()
            .AddEndpointsApiExplorer()
            .AddSwaggerGen()
        |> ignore
    
    /// Configure application pipeline
    let private configureApp (weatherConfig: WeatherConfig) (app: WebApplication) : unit =
        // Create dependencies and partially apply
        let deps = createDefaultDependencies weatherConfig
        let generateForecasts = generateWeatherForecasts deps
        
        // Create endpoints with injected dependencies
        let endpoints = createEndpoints generateForecasts
        
        // Configure middleware
        if app.Environment.IsDevelopment() then
            app.UseDeveloperExceptionPage() |> ignore
        else
            app.UseExceptionHandler("/error") |> ignore
        
        app.UseStaticFiles()
        |> _.UseAntiforgery()
        |> _.UseRouting()
        |> _.UseOxpecker(endpoints)
        |> _.UseSwagger()
        |> _.UseSwaggerUI()
        |> ignore
        
        app.MapOpenApi() |> ignore

    [<EntryPoint>]
    let main args =
        let builder = WebApplication.CreateBuilder(args)
        
        // Load configuration
        let weatherConfig = loadWeatherConfig builder.Configuration
        
        // Configure services
        configureServices builder.Services
        
        let app = builder.Build()
        
        // Configure app with dependencies
        configureApp weatherConfig app
        
        app.Run()
        
        ExitCode.Success |> int
```

## Benefits of This Refactoring

### 1. **Testability**

#### Before:
```fsharp
// Cannot test without HttpContext mock
// Cannot control DateTime.Now or Random
// All or nothing testing
```

#### After:
```fsharp
// Domain tests - pure, no dependencies
[<Test>]
let ``validateCount rejects negative numbers`` () =
    let result = Weather.validateCount -1
    Assert.IsTrue(Result.isError result)

[<Test>]
let ``generateForecasts creates correct number`` () =
    let summaries = ["Cold"; "Warm"]
    let temps = [| 10; 20; 15 |]
    let indices = [| 0; 1; 0 |]
    let startDate = DateTime(2025, 1, 1)
    
    let result = 
        Weather.generateForecasts 
            summaries 
            temps 
            indices 
            startDate 
            3
    
    Assert.AreEqual(3, result.Length)
    Assert.AreEqual("Cold", result.[0].Summary)
    Assert.AreEqual(20, result.[1].TemperatureC)

// Application tests - control dependencies
[<Test>]
let ``service handles invalid count`` () =
    let deps = {
        GetCurrentTime = fun () -> DateTime(2025, 1, 1)
        GetRandomInt = fun _ _ -> 10
        Config = testConfig
    }
    
    let result = generateWeatherForecasts deps 0
    Assert.IsTrue(Result.isError result)
```

### 2. **Separation of Concerns**
- **Domain**: Pure business logic, no frameworks
- **Application**: Orchestration, manages effects
- **HTTP**: Only HTTP concerns, thin layer

### 3. **Reusability**
- Domain functions can be used in CLI, tests, other APIs
- Not tied to Oxpecker or ASP.NET Core

### 4. **Maintainability**
- Each layer has single responsibility
- Easy to find and modify code
- Clear dependencies

### 5. **Flexibility**
- Easy to swap HTTP framework
- Easy to change random number generation
- Easy to add caching, logging, etc.

### 6. **Type Safety**
- Compile-time guarantees
- Explicit error handling with Result types
- No runtime surprises

## Testing Strategy

### Domain Layer (95% of tests)
- Unit tests for all pure functions
- Property-based testing
- Fast, no I/O

### Application Layer (Some tests)
- Test with mock dependencies
- Integration tests

### HTTP Layer (Minimal tests)
- Mostly integration tests
- Test error mapping

## Incremental Migration

You can adopt this gradually:

1. **Start**: Extract pure domain functions
2. **Next**: Create application service layer
3. **Then**: Make handlers thin
4. **Finally**: Separate OpenAPI specs

## Key Takeaways

✅ **Do:**
- Keep domain pure and framework-agnostic
- Inject dependencies as functions
- Use Result types for domain errors
- Separate HTTP concerns from business logic
- Make impure operations explicit

❌ **Don't:**
- Mix HTTP and business logic
- Use DateTime.Now or Random in domain
- Make HttpContext available to domain
- Put OpenAPI specs in handler code
- Make handlers do too much

