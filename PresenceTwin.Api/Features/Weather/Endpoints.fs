namespace PresenceTwin.Api.Features.Weather.Endpoints

open System
open Oxpecker
open PresenceTwin.Api.Infrastructure.Configuration

module Endpoints =

    // ==================== DEPENDENCY CREATION ====================
    
    /// Create production dependencies for GetForecast query
    let private createGetForecastDeps (config: WeatherConfig) : PresenceTwin.Api.Features.Weather.GetForecast.Dependencies =
        {
            Config = config
            GetCurrentTime = fun () -> DateTime.UtcNow
            GetRandomInt = fun min max -> Random.Shared.Next(min, max)
        }
    
    /// Create production dependencies for GenerateForecasts command
    let private createGenerateForecastsDeps (config: WeatherConfig) : PresenceTwin.Api.Features.Weather.GenerateForecasts.Dependencies =
        {
            Config = config
            GetCurrentTime = fun () -> DateTime.UtcNow
            GetRandomInt = fun min max -> Random.Shared.Next(min, max)
        }
    
    // ==================== ENDPOINT CONFIGURATION ====================
    
    /// Create all weather endpoints with dependencies injected
    let endpoints (config: WeatherConfig) : Endpoint list =
        // Create dependencies (partial application / DI composition root)
        let queryDeps = createGetForecastDeps config
        let commandDeps = createGenerateForecastsDeps config
        
        [
            // Query: GET /api/weather/forecast/{count}
            GET [
                routef "/api/weather/forecast/%i" (fun count ->
                    PresenceTwin.Api.Features.Weather.GetForecast.GetForecast.httpHandler queryDeps count
                )
                |> PresenceTwin.Api.Features.Weather.GetForecast.GetForecast.configureOpenApi
            ]
            
            // Command: POST /api/weather/forecast/generate
            POST [
                route "/api/weather/forecast/generate" (PresenceTwin.Api.Features.Weather.GenerateForecasts.GenerateForecasts.httpHandler commandDeps)
                |> PresenceTwin.Api.Features.Weather.GenerateForecasts.GenerateForecasts.configureOpenApi
            ]
        ]

