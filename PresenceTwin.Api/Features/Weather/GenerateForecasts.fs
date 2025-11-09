namespace PresenceTwin.Api.Features.Weather

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Oxpecker
open Oxpecker.OpenApi
open Microsoft.OpenApi.Models
open PresenceTwin.Api.Common
open PresenceTwin.Api.Infrastructure

module GenerateForecasts =
    
    // ==================== COMMAND MODEL ====================
    
    /// Command input
    type Command = {
        Count: int
        StartDate: DateTime option
    }
    
    /// Command result
    type CommandResult = {
        Forecasts: Domain.WeatherForecast array
        GeneratedAt: DateTime
        Count: int
    }
    
    /// Command errors
    type CommandError =
        | InvalidCount of int * string
        | InvalidStartDate of string
        | GenerationFailed of string
    
    // ==================== DEPENDENCIES ====================
    
    /// Dependencies needed by this command
    type Dependencies = {
        Config: Configuration.WeatherConfig
        TimeProvider: Dependencies.ITimeProvider
        RandomProvider: Dependencies.IRandomProvider
    }
    
    // ==================== VALIDATION (Pure) ====================
    
    /// Validate count
    let private validateCount (maxDays: int) (count: int) : Result<int, CommandError> =
        if count < 1 then
            Error (InvalidCount (count, "Count must be at least 1"))
        elif count > maxDays then
            Error (InvalidCount (count, $"Count cannot exceed {maxDays}"))
        else
            Ok count
    
    /// Validate start date (if provided)
    let private validateStartDate (startDate: DateTime option) : Result<DateTime option, CommandError> =
        match startDate with
        | Some date when date > DateTime.UtcNow.AddYears(1) ->
            Error (InvalidStartDate "Start date cannot be more than 1 year in the future")
        | Some date when date < DateTime.UtcNow.AddYears(-1) ->
            Error (InvalidStartDate "Start date cannot be more than 1 year in the past")
        | _ -> Ok startDate
    
    /// Validate the entire command
    let private validate (config: Configuration.WeatherConfig) (command: Command) : Result<Command, CommandError> =
        Result.validation {
            let! _ = validateCount config.MaxForecastDays command.Count
            let! _ = validateStartDate command.StartDate
            return command
        }
    
    // ==================== BUSINESS LOGIC (Orchestration) ====================
    
    /// Execute the command (coordinates pure domain logic with impure I/O)
    let private execute (deps: Dependencies) (command: Command) : CommandResult =
        // Get start date (use provided or current time)
        let startDate = 
            command.StartDate 
            |> Option.defaultWith (fun () -> deps.TimeProvider.GetCurrentTime())
        
        let generatedAt = deps.TimeProvider.GetCurrentTime()
        
        // Generate random temperatures (impure, injected)
        let temperatures = 
            [| for _ in 1 .. command.Count do
                deps.RandomProvider.GetInt deps.Config.MinTemperature deps.Config.MaxTemperature |]
        
        // Generate random summary indices (impure, injected)
        let summaryIndices=
            [| for _ in 1 .. command.Count do
                deps.RandomProvider.GetInt 0 deps.Config.Summaries.Length |]
        
        // Call pure domain function
        let forecasts =
            Domain.generateForecasts
                deps.Config.Summaries
                temperatures
                summaryIndices
                startDate
                command.Count
        
        { Forecasts = forecasts
          GeneratedAt = generatedAt
          Count = command.Count }
    
    // ==================== HANDLER ====================
    
    /// Main command handler
    let handle (deps: Dependencies) (command: Command) : Result<CommandResult, CommandError> =
        command
        |> validate deps.Config
        |> Result.map (execute deps)
    
    // ==================== HTTP LAYER ====================
    
    /// HTTP request body
    type GenerateForecastsRequest = {
        Count: int
        StartDate: DateTime option
    }
    
    /// Map domain errors to HTTP responses
    let private mapErrorToHttp (error: CommandError) (ctx: HttpContext) : Task =
        match error with
        | InvalidCount (count, message) ->
            Http.writeBadRequest "InvalidCount" message (Some {| count = count |}) ctx
        | InvalidStartDate message ->
            Http.writeBadRequest "InvalidStartDate" message None ctx
        | GenerationFailed message ->
            Http.writeInternalServerError message ctx
    
    /// HTTP endpoint handler
    let httpHandler (deps: Dependencies) (ctx: HttpContext) : Task =
        task {
            // Parse request body
            let! request = ctx.BindJson<GenerateForecastsRequest>()
            
            let command : Command = {
                Count = request.Count
                StartDate = request.StartDate
            }
            
            return!
                handle deps command
                |> Http.writeResult 
                    (fun result -> Http.writeJsonCreated result None)
                    mapErrorToHttp
                <| ctx
        }
    
    // ==================== OPENAPI SPECIFICATION ====================
    
    /// Configure OpenAPI for this endpoint
    let configureOpenApi (endpoint: Endpoint) : Endpoint =
        endpoint
        |> addOpenApi (
            OpenApiConfig(
                requestBody = RequestBody(typeof<GenerateForecastsRequest>),
                responseBodies = [|
                    ResponseBody(typeof<CommandResult>, statusCode = 201)
                    ResponseBody(typeof<Http.ErrorResponse>, statusCode = 400)
                    ResponseBody(typeof<Http.ErrorResponse>, statusCode = 500)
                |],
                configureOperation = fun operation ->
                    operation.Summary <- "Generate weather forecasts"
                    operation.Description <- "Generate new weather forecasts for the specified number of days"
                    operation.OperationId <- "GenerateWeatherForecasts"
                    operation.Tags <- System.Collections.Generic.List<OpenApiTag>([ OpenApiTag(Name = "Weather") ])
                    operation
            )
        )
