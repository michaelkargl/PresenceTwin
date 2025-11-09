namespace PresenceTwin.Api.Features.Weather

open Oxpecker
open PresenceTwin.Api.Infrastructure

module Endpoints =
    
    /// Create all weather endpoints with dependencies injected
    let endpoints (config: Configuration.WeatherConfig) : Endpoint list =
        // Create dependencies for queries
        let queryDeps : GetForecast.Dependencies = {
            Config = config
            TimeProvider = Dependencies.createTimeProvider()
            RandomProvider = Dependencies.createRandomProvider()
        }
        
        // Create dependencies for commands
        let commandDeps : GenerateForecasts.Dependencies = {
            Config = config
            TimeProvider = Dependencies.createTimeProvider()
            RandomProvider = Dependencies.createRandomProvider()
        }
        
        [
            // Query: GET /api/weather/forecast/{count}
            GET [
                routef "/api/weather/forecast/%i" (fun count ->
                    GetForecast.httpHandler queryDeps count
                )
                |> GetForecast.configureOpenApi
            ]
            
            // Command: POST /api/weather/forecast/generate
            POST [
                route "/api/weather/forecast/generate" (GenerateForecasts.httpHandler commandDeps)
                |> GenerateForecasts.configureOpenApi
            ]
        ]
