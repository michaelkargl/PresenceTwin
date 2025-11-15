namespace PresenceTwin.Api

open Oxpecker
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open PresenceTwin.Api.Infrastructure.Configuration
open PresenceTwin.Api.Features.Weather.Endpoints

type ExitCode =
    | Success = 0
    | GeneralError = 1

module Program =

    /// Configure application services (DI container)
    let private configureServices (services: IServiceCollection) : unit =
        services.AddRouting().AddAntiforgery().AddOxpecker().AddOpenApi().AddEndpointsApiExplorer().AddSwaggerGen()
        |> ignore

    /// application middleware pipeline
    let private configureApp (config: WeatherConfig) (app: WebApplication) : unit =
        if app.Environment.IsDevelopment() then
            app.UseDeveloperExceptionPage() |> ignore
        else
            app.UseExceptionHandler("/error") |> ignore

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
        let weatherConfig = Configuration.loadWeatherConfig builder.Configuration

        configureServices builder.Services

        let app = builder.Build()

        configureApp weatherConfig app

        app.Run()
        ExitCode.Success |> int
