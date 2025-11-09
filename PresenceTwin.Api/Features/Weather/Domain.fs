namespace PresenceTwin.Api.Features.Weather

open System

module Domain =
    
    /// Weather forecast domain model
    type WeatherForecast = {
        Date: DateTime
        TemperatureC: int
        Summary: string
    }
    with
        /// Computed property for Fahrenheit temperature
        member this.TemperatureF =
            32.0 + (float this.TemperatureC / 0.5556)
    
    /// Create a weather forecast (pure function)
    let createForecast (date: DateTime) (temperatureC: int) (summary: string) : WeatherForecast =
        { Date = date
          TemperatureC = temperatureC
          Summary = summary }
    
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
            createForecast
                (startDate.AddDays(float (i + 1)))
                temperatures.[i]
                summaries.[summaryIndices.[i]] |]
