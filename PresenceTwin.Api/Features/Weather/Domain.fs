namespace PresenceTwin.Api.Features.Weather.Domain

open System

type WeatherForecast = {
    Date: DateTime
    TemperatureC: int
    Summary: string
}

module Domain =
    module WeatherForecast =
        
        let temperatureF (forecast: WeatherForecast) : float =
            32.0 + (float forecast.TemperatureC / 0.5556)
        
        let create (date: DateTime) (temperatureC: int) (summary: string) : WeatherForecast =
            { Date = date
              TemperatureC = temperatureC
              Summary = summary }
    
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
