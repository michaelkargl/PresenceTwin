namespace PresenceTwin.Api.Common.Http

open Oxpecker
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open PresenceTwin.Api.Common.Error.Http

module Http =
    let writeOk<'T> (data: 'T) (ctx: HttpContext) : Task =
        ctx.SetStatusCode(200)
        ctx.WriteJson(data)

    let writeNoContent (ctx: HttpContext) : Task =
        ctx.SetStatusCode(204)
        Task.CompletedTask

    let writeBadRequest (error: string) (message: string) (details: obj option) (ctx: HttpContext) : Task =
        ctx.SetStatusCode(400)
        ctx.WriteJson(ErrorResponse.createError error message details)

    let writeNotFound (message: string) (ctx: HttpContext) : Task =
        ctx.SetStatusCode(404)
        ctx.WriteJson(ErrorResponse.createError "NotFound" message None)

    let writeConflict (message: string) (ctx: HttpContext) : Task =
        ctx.SetStatusCode(409)
        ctx.WriteJson(ErrorResponse.createError "Conflict" message None)

    let writeValidationError (errors: obj) (ctx: HttpContext) : Task =
        ctx.SetStatusCode(422)
        ctx.WriteJson(ErrorResponse.createError "ValidationError" "Validation failed" (Some errors))

    let writeInternalServerError (message: string) (ctx: HttpContext) : Task =
        ctx.SetStatusCode(500)
        ctx.WriteJson(ErrorResponse.createError "InternalServerError" message None)

    let writeResult<'T, 'E>
        (successWriter: 'T -> HttpContext -> Task)
        (errorMapper: 'E -> HttpContext -> Task)
        (result: Result<'T, 'E>)
        (ctx: HttpContext)
        : Task =
        match result with
        | Ok value -> successWriter value ctx
        | Error error -> errorMapper error ctx

    let handleExceptions (handler: HttpContext -> Task) (ctx: HttpContext) : Task =
        task {
            try
                return! handler ctx
            with ex ->
                // TODO: In production, log the exception here
                return! writeInternalServerError ex.Message ctx
        }
