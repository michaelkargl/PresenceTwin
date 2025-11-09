namespace PresenceTwin.Api.Common

open System

module Validation =
    
    /// Validation error type
    type ValidationError = {
        Field: string
        Message: string
    }
    
    /// Create a validation error
    let error field message = {
        Field = field
        Message = message
    }
    
    /// Validate that a value is not null or whitespace
    let notEmpty (field: string) (value: string) : Result<string, ValidationError> =
        if String.IsNullOrWhiteSpace(value) then
            Error (error field $"{field} cannot be empty")
        else
            Ok value
    
    /// Validate that a number is positive
    let positive (field: string) (value: int) : Result<int, ValidationError> =
        if value <= 0 then
            Error (error field $"{field} must be positive")
        else
            Ok value
    
    /// Validate that a number is non-negative
    let nonNegative (field: string) (value: int) : Result<int, ValidationError> =
        if value < 0 then
            Error (error field $"{field} cannot be negative")
        else
            Ok value
    
    /// Validate that a number is within a range
    let inRange (field: string) (min: int) (max: int) (value: int) : Result<int, ValidationError> =
        if value < min || value > max then
            Error (error field $"{field} must be between {min} and {max}")
        else
            Ok value
    
    /// Validate that a string has a maximum length
    let maxLength (field: string) (max: int) (value: string) : Result<string, ValidationError> =
        if String.IsNullOrEmpty(value) then
            Ok value
        elif value.Length > max then
            Error (error field $"{field} cannot exceed {max} characters")
        else
            Ok value
    
    /// Validate that a date is not in the past
    let notInPast (field: string) (date: DateTime) : Result<DateTime, ValidationError> =
        if date < DateTime.UtcNow then
            Error (error field $"{field} cannot be in the past")
        else
            Ok date
    
    /// Validate that a date is not in the future
    let notInFuture (field: string) (date: DateTime) : Result<DateTime, ValidationError> =
        if date > DateTime.UtcNow then
            Error (error field $"{field} cannot be in the future")
        else
            Ok date
    
    /// Combine multiple validation results
    let combine (results: Result<'T, ValidationError> list) : Result<'T list, ValidationError list> =
        let errors = results |> List.choose (function Error e -> Some e | _ -> None)
        
        if List.isEmpty errors then
            results |> List.choose (function Ok v -> Some v | _ -> None) |> Ok
        else
            Error errors
    
    /// Validate all and return first error or success
    let validateAll (validations: (unit -> Result<'T, ValidationError>) list) : Result<'T list, ValidationError> =
        validations
        |> List.fold (fun state validate ->
            match state with
            | Error e -> Error e
            | Ok values ->
                match validate() with
                | Ok value -> Ok (value :: values)
                | Error e -> Error e
        ) (Ok [])
        |> Result.map List.rev
