namespace PresenceTwin.Api

#nowarn "20"

open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Threading.Tasks
open Microsoft.AspNetCore
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.HttpsPolicy
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Oxpecker

type ExitCode =
    | Success = 0
    | GeneralError = 1

module Program =
    let exitCode = 0
    
    let private endpoints = [
        GET [
            route "/weatherforecast" <| Controllers.getWeatherData
        ]
    ]

    let private configureApp (app: WebApplication) : unit =
        if app.Environment.IsDevelopment() then
            app.UseDeveloperExceptionPage() |> ignore
        else
            app.UseExceptionHandler("/error") |> ignore
        
        app.UseStaticFiles().UseAntiforgery().UseRouting().UseOxpecker(endpoints) |> ignore
    
    let private configureServices (services: IServiceCollection) : unit =
        services.AddRouting().AddAntiforgery().AddOxpecker() |> ignore

    [<EntryPoint>]
    let main args =

        let builder = WebApplication.CreateBuilder(args)
        configureServices builder.Services

        let app = builder.Build()
        configureApp app
        
        app.Run()

        ExitCode.Success |> int
