module PresenceTwin.Api.Controllers

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Oxpecker
open PresenceTwin.Api

let getWeatherData (ctx: HttpContext): Task =
    task {
        // Simulate asynchronous loading to demonstrate long rendering
        do! Task.Delay(1)

        let startDate = DateTime.Now
        let summaries = [ "Freezing"; "Bracing"; "Chilly"; "Cool"; "Mild"; "Warm"; "Balmy"; "Hot"; "Sweltering"; "Scorching" ]
        let forecasts =
            [|
                for index in 1..5 do
                    {
                        Date = startDate.AddDays(index)
                        TemperatureC = Random.Shared.Next(-20, 55)
                        Summary = summaries[Random.Shared.Next(summaries.Length)]
                    }
            |]
        return! forecasts |> ctx.WriteJson
    } :> Task