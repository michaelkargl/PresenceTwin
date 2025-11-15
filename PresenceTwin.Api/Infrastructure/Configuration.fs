namespace PresenceTwin.Api.Infrastructure.Configuration

open Microsoft.Extensions.Configuration

type WeatherConfig = {
    Summaries: string list
    MinTemperature: int
    MaxTemperature: int
    MaxForecastDays: int
}

module Configuration =    
    let loadWeatherConfig (config: IConfiguration) : WeatherConfig =
        let summaries = 
            config.GetSection("Weather:Summaries").Get<string[]>()
            |> Option.ofObj
            |> Option.defaultValue [|
                "Freezing"; "Bracing"; "Chilly"; "Cool"; "Mild"
                "Warm"; "Balmy"; "Hot"; "Sweltering"; "Scorching"
            |]
            |> Array.toList
        
        let minTemp = 
            config.GetValue<int>("Weather:MinTemperature")
            |> fun v -> if v = 0 then -20 else v
        
        let maxTemp = 
            config.GetValue<int>("Weather:MaxTemperature")
            |> fun v -> if v = 0 then 55 else v
        
        let maxDays = 
            config.GetValue<int>("Weather:MaxForecastDays")
            |> fun v -> if v = 0 then 100 else v
        
        { Summaries = summaries
          MinTemperature = minTemp
          MaxTemperature = maxTemp
          MaxForecastDays = maxDays }
