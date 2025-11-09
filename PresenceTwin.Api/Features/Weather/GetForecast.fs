namespace PresenceTwin.Api.Features.Weather

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Oxpecker
open Oxpecker.OpenApi
open Microsoft.OpenApi.Models
open PresenceTwin.Api.Common
open PresenceTwin.Api.Infrastructure

module GetForecast =
    
    // ==================== QUERY MODEL ====================
    
    /// Query input
    type Query = {
        Count: int
    }
    
    /// Query result
    type QueryResult = Domain.WeatherForecast array
    
    /// Query errors
    type QueryError =
        | InvalidCount of int * string
        | ConfigurationError of string
    
    // ==================== DEPENDENCIES ====================
    
    /// Dependencies needed by this query
    type Dependencies = {
        Config: Configuration.WeatherConfig
        TimeProvider: Dependencies.ITimeProvider
        RandomProvider: Dependencies.IRandomProvider
    }
    
    // ==================== VALIDATION (Pure) ====================
    
    /// Validate the query
    let private validate (config: Configuration.WeatherConfig) (query: Query) : Result<Query, QueryError> =
        if query.Count < 1 then
            Error (InvalidCount (query.Count, "Count must be at least 1"))
        elif query.Count > config.MaxForecastDays then
            Error (InvalidCount (query.Count, $"Count cannot exceed {config.MaxForecastDays}"))
        else
            Ok query
    
    // ==================== BUSINESS LOGIC (Orchestration) ====================
    
    /// Execute the query (coordinates pure domain logic with impure I/O)
    let private execute (deps: Dependencies) (query: Query) : QueryResult =
        // Get current time (impure, injected)
        let startDate = deps.TimeProvider.GetCurrentTime()
        
        // Generate random temperatures (impure, injected)
        let temperatures = 
            [| for _ in 1 .. query.Count do
                deps.RandomProvider.GetInt deps.Config.MinTemperature deps.Config.MaxTemperature |]
        
        // Generate random summary indices (impure, injected)
        let summaryIndices =
            [| for _ in 1 .. query.Count do
                deps.RandomProvider.GetInt 0 deps.Config.Summaries.Length |]
        
        // Call pure domain function
        Domain.generateForecasts
            deps.Config.Summaries
            temperatures
            summaryIndices
            startDate
            query.Count
    
    // ==================== HANDLER ====================
    
    /// Main query handler
    let handle (deps: Dependencies) (query: Query) : Result<QueryResult, QueryError> =
        query
        |> validate deps.Config
        |> Result.map (execute deps)
    
    // ==================== HTTP LAYER ====================
    
    /// Map domain errors to HTTP responses
    let private mapErrorToHttp (error: QueryError) (ctx: HttpContext) : Task =
        match error with
        | InvalidCount (count, message) ->
            Http.writeBadRequest "InvalidCount" message (Some {| count = count |}) ctx
        | ConfigurationError message ->
            Http.writeInternalServerError message ctx
    
    /// HTTP endpoint handler
    let httpHandler (deps: Dependencies) (count: int) (ctx: HttpContext) : Task =
        let query = { Count = count }
        
        handle deps query
        |> Http.writeResult Http.writeJsonOk mapErrorToHttp
        <| ctx
    
    // ==================== OPENAPI SPECIFICATION ====================
    
    /// Configure OpenAPI for this endpoint
    let configureOpenApi (endpoint: Endpoint) : Endpoint =
        endpoint
        |> addOpenApi (
            OpenApiConfig(
                responseBodies = [|
                    ResponseBody(typeof<Domain.WeatherForecast[]>, statusCode = 200)
                    ResponseBody(typeof<Http.ErrorResponse>, statusCode = 400)
                    ResponseBody(typeof<Http.ErrorResponse>, statusCode = 500)
                |],
                configureOperation = fun operation ->
                    operation.Summary <- "Get weather forecasts"
                    operation.Description <- "Retrieve weather forecasts for the specified number of days (1-100)"
                    operation.OperationId <- "GetWeatherForecast"
                    operation.Tags <- System.Collections.Generic.List<OpenApiTag>([ OpenApiTag(Name = "Weather") ])
                    
                    operation.Parameters <- System.Collections.Generic.List<OpenApiParameter>([
                        OpenApiParameter(
                            Name = "count",
                            In = ParameterLocation.Path,
                            Required = true,
                            Description = "Number of forecast days",
                            Schema = OpenApiSchema(
                                Type = "integer",
                                Format = "int32",
                                Minimum = Nullable(1.0m),
                                Maximum = Nullable(100.0m)
                            )
                        )
                    ])
                    operation
            )
        )
