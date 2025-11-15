namespace PresenceTwin.Api.Features.Weather.Domain

open System

// ==================== DOMAIN TYPES ====================

/// Weather forecast domain model (pure data, no behavior)
type WeatherForecast = {
    Date: DateTime
    TemperatureC: int
    Summary: string
}

// ==================== MODULE ====================

module Domain =

    // ==================== DOMAIN FUNCTIONS ====================
    
    /// Module containing functions for WeatherForecast type
    module WeatherForecast =
        
        /// Calculate Fahrenheit temperature from Celsius (pure function)
        let temperatureF (forecast: WeatherForecast) : float =
            32.0 + (float forecast.TemperatureC / 0.5556)
        
        /// Create a weather forecast (pure function)
        let create (date: DateTime) (temperatureC: int) (summary: string) : WeatherForecast =
            { Date = date
              TemperatureC = temperatureC
              Summary = summary }
    
    // ==================== BUSINESS LOGIC ====================
    
    /// Generate forecasts from provided inputs (pure function)
    /// All impure operations (random numbers, current time) are passed as parameters
    let generateForecasts
        (summaries: string list)
        (temperatures: int array)
        (summaryIndices: int array)
        (startDate: DateTime)
        (count: int)
        : WeatherForecast array =
        
        [| for i in 0 .. count - 1 do
            WeatherForecast.create
                (startDate.AddDays(float (i + 1)))
                temperatures[i]
                summaries[summaryIndices[i]] |]
