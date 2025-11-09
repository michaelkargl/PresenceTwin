namespace PresenceTwin.Api.Common

module Result =
    
    /// Map a function over the Ok value
    let map (f: 'T -> 'U) (result: Result<'T, 'E>) : Result<'U, 'E> =
        match result with
        | Ok value -> Ok (f value)
        | Error error -> Error error
    
    /// Bind a function that returns a Result over the Ok value
    let bind (f: 'T -> Result<'U, 'E>) (result: Result<'T, 'E>) : Result<'U, 'E> =
        match result with
        | Ok value -> f value
        | Error error -> Error error
    
    /// Map a function over the Error value
    let mapError (f: 'E1 -> 'E2) (result: Result<'T, 'E1>) : Result<'T, 'E2> =
        match result with
        | Ok value -> Ok value
        | Error error -> Error (f error)
    
    /// Apply a function wrapped in a Result to a value wrapped in a Result
    let apply (fResult: Result<'T -> 'U, 'E>) (result: Result<'T, 'E>) : Result<'U, 'E> =
        match fResult, result with
        | Ok f, Ok value -> Ok (f value)
        | Error e, _ -> Error e
        | _, Error e -> Error e
    
    /// Check if Result is Ok
    let isOk (result: Result<'T, 'E>) : bool =
        match result with
        | Ok _ -> true
        | Error _ -> false
    
    /// Check if Result is Error
    let isError (result: Result<'T, 'E>) : bool =
        not (isOk result)
    
    /// Get the Ok value or a default
    let defaultValue (defaultVal: 'T) (result: Result<'T, 'E>) : 'T =
        match result with
        | Ok value -> value
        | Error _ -> defaultVal
    
    /// Get the Ok value or compute a default from the error
    let defaultWith (f: 'E -> 'T) (result: Result<'T, 'E>) : 'T =
        match result with
        | Ok value -> value
        | Error error -> f error
    
    /// Combine multiple Results into a single Result containing a list
    let sequence (results: Result<'T, 'E> list) : Result<'T list, 'E> =
        let folder (state: Result<'T list, 'E>) (result: Result<'T, 'E>) =
            match state, result with
            | Ok values, Ok value -> Ok (value :: values)
            | Error e, _ -> Error e
            | _, Error e -> Error e
        
        List.fold folder (Ok []) results
        |> map List.rev
    
    /// Lift a value into a Result
    let ofOption (error: 'E) (option: 'T option) : Result<'T, 'E> =
        match option with
        | Some value -> Ok value
        | None -> Error error
    
    /// Validation builder
    type ValidationBuilder() =
        member _.Return(x) = Ok x
        member _.Bind(x, f) = bind f x
        member _.ReturnFrom(x) = x
        
    let validation = ValidationBuilder()
