namespace PresenceTwin.Api

#nowarn "20"

open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Oxpecker
open Oxpecker.OpenApi
open PresenceTwin.Api.Infrastructure
open PresenceTwin.Api.Features.Weather

type ExitCode =
    | Success = 0
    | GeneralError = 1

module Program =

    /// Configure application services (DI container)
    let private configureServices (services: IServiceCollection) : unit =
        services
            .AddRouting()
            .AddAntiforgery()
            .AddOxpecker()
            .AddOpenApi()
            .AddEndpointsApiExplorer()
            .AddSwaggerGen()
        |> ignore

    /// Configure application middleware pipeline
    let private configureApp (config: Configuration.WeatherConfig) (app: WebApplication) : unit =
        // Configure middleware
        if app.Environment.IsDevelopment() then
            app.UseDeveloperExceptionPage() |> ignore
        else
            app.UseExceptionHandler("/error") |> ignore

        // Get all endpoints from features
        app.UseStaticFiles()
        |> _.UseAntiforgery()
        |> _.UseRouting()
        |> _.UseOxpecker([ yield! Endpoints.endpoints config ])
        |> _.UseSwagger()
        |> _.UseSwaggerUI()
        |> ignore

        app.MapOpenApi() |> ignore

    [<EntryPoint>]
    let main args =
        let builder = WebApplication.CreateBuilder(args)

        // Load configuration
        let weatherConfig = Configuration.loadWeatherConfig builder.Configuration

        // Configure services
        configureServices builder.Services

        let app = builder.Build()

        // Configure app with dependencies
        configureApp weatherConfig app

        app.Run()
        ExitCode.Success |> int
