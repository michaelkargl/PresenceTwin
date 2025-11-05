namespace PresenceTwin.Api

#nowarn "20"

open System.Collections.Generic
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.OpenApi.Models
open Oxpecker
open Oxpecker.OpenApi

type ExitCode = 
    | Success = 0
    | GeneralError = 1

type TestHttpError = { Code: string; Message: string }

module Program =
    let private endpoints =
        [ GET
              [ routef "/weatherforecast/{%i:int}" Controllers.getWeatherData
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
                ) ] ]

    let private configureApp (app: WebApplication) : unit =
        if app.Environment.IsDevelopment() then
            app.UseDeveloperExceptionPage() |> ignore
        else
            app.UseExceptionHandler("/error") |> ignore

        app.UseStaticFiles()
        |> _.UseAntiforgery()
        |> _.UseRouting()
        |> _.UseOxpecker(endpoints)
        |> _.UseSwagger()
        |> _.UseSwaggerUI()
        |> ignore

        app.MapOpenApi() |> ignore

    let private configureServices (services: IServiceCollection) : unit =
        services
            .AddRouting()
            .AddAntiforgery()
            .AddOxpecker()
            .AddOpenApi()
            .AddEndpointsApiExplorer()
            .AddSwaggerGen()
        |> ignore

    [<EntryPoint>]
    let main args =

        let builder = WebApplication.CreateBuilder(args)
        configureServices builder.Services

        let app = builder.Build()
        configureApp app

        app.Run()

        ExitCode.Success |> int
