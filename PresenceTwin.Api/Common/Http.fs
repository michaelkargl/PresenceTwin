namespace PresenceTwin.Api.Common

open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Oxpecker

module Http =
    
    /// Standard error response
    type ErrorResponse = {
        Error: string
        Message: string
        Details: obj option
    }
    
    /// Create an error response
    let createError error message details =
        { Error = error
          Message = message
          Details = details }
    
    /// Write JSON response with 200 OK
    let writeJsonOk<'T> (data: 'T) (ctx: HttpContext) : Task =
        ctx.SetStatusCode(200)
        ctx.WriteJson(data)
    
    /// Write JSON response with 201 Created
    let writeJsonCreated<'T> (data: 'T) (location: string option) (ctx: HttpContext) : Task =
        ctx.SetStatusCode(201)
        match location with
        | Some loc -> ctx.Response.Headers.["Location"] <- loc
        | None -> ()
        ctx.WriteJson(data)
    
    /// Write 204 No Content
    let writeNoContent (ctx: HttpContext) : Task =
        ctx.SetStatusCode(204)
        Task.CompletedTask
    
    /// Write 400 Bad Request with error details
    let writeBadRequest (error: string) (message: string) (details: obj option) (ctx: HttpContext) : Task =
        ctx.SetStatusCode(400)
        ctx.WriteJson(createError error message details)
    
    /// Write 404 Not Found
    let writeNotFound (message: string) (ctx: HttpContext) : Task =
        ctx.SetStatusCode(404)
        ctx.WriteJson(createError "NotFound" message None)
    
    /// Write 409 Conflict
    let writeConflict (message: string) (ctx: HttpContext) : Task =
        ctx.SetStatusCode(409)
        ctx.WriteJson(createError "Conflict" message None)
    
    /// Write 422 Unprocessable Entity (validation errors)
    let writeValidationError (errors: obj) (ctx: HttpContext) : Task =
        ctx.SetStatusCode(422)
        ctx.WriteJson(createError "ValidationError" "Validation failed" (Some errors))
    
    /// Write 500 Internal Server Error
    let writeInternalServerError (message: string) (ctx: HttpContext) : Task =
        ctx.SetStatusCode(500)
        ctx.WriteJson(createError "InternalServerError" message None)
    
    /// Map Result to HTTP response
    let writeResult<'T, 'E> 
        (successWriter: 'T -> HttpContext -> Task)
        (errorMapper: 'E -> HttpContext -> Task)
        (result: Result<'T, 'E>)
        (ctx: HttpContext)
        : Task =
        match result with
        | Ok value -> successWriter value ctx
        | Error error -> errorMapper error ctx
    
    /// Try-catch wrapper that returns 500 on exception
    let handleExceptions (handler: HttpContext -> Task) (ctx: HttpContext) : Task =
        task {
            try
                return! handler ctx
            with ex ->
                // In production, log the exception here
                return! writeInternalServerError ex.Message ctx
        }
