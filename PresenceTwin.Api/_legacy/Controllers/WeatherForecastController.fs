module PresenceTwin.Api.Controllers

open System
open System.Threading.Tasks
open System.Collections.Generic
open Microsoft.AspNetCore.Http
open Microsoft.OpenApi.Models
open Oxpecker
open Oxpecker.OpenApi
open PresenceTwin.Api
open PresenceTwin.Api.Errors.Http




module Controllers =
    module GetWeatherData =
        let getWeatherDataHandler (count: int) (ctx: HttpContext) : Task =
            task {
                // Simulate asynchronous loading to demonstrate long rendering
                do! Task.Delay(1)

                let startDate = DateTime.Now

                let summaries =
                    [ "Freezing"
                      "Bracing"
                      "Chilly"
                      "Cool"
                      "Mild"
                      "Warm"
                      "Balmy"
                      "Hot"
                      "Sweltering"
                      "Scorching" ]

                let forecasts =
                    [| for index in 1..count do
                           { Date = startDate.AddDays(index)
                             TemperatureC = Random.Shared.Next(-20, 55)
                             Summary = summaries[Random.Shared.Next(summaries.Length)] } |]

                return! forecasts |> ctx.WriteJson
            }

        let addOpenTehApiSpec (endpoint: Endpoint) : Endpoint =
            endpoint
            |> addOpenApi (
                OpenApiConfig(
                    responseBodies =
                        [| ResponseBody(typeof<string>)
                           ResponseBody(typeof<TestHttpError>, statusCode = 500) |],
                    configureOperation =
                        (fun o ->
                            o.Summary <- "Get weather forecast"
                            o.Description <- "Retrieve a list of weather forecasts"

                            o.Parameters <-
                                List<OpenApiParameter>(
                                    [ OpenApiParameter(
                                          Name = "count",
                                          In = ParameterLocation.Path,
                                          Required = true,
                                          Deprecated = true,
                                          Description = "The number of forecasts",
                                          Schema = OpenApiSchema(Type = "integer", Format = "int32")
                                      ) ]
                                )

                            o)
                )
            )
