# F# Functional API Best Practices - Quick Reference

## Project Structure

```
PresenceTwin.Api/
├── Common/                     # Shared utilities (no dependencies)
├── Infrastructure/             # Cross-cutting concerns
└── Features/                   # Vertical slices (CQRS)
    └── [Feature]/
        ├── Domain.fs          # Pure domain types & logic
        ├── [Query].fs         # Read operations
        ├── [Command].fs       # Write operations
        └── Endpoints.fs       # HTTP routing & DI
```

## Naming Convention

```
File Path                           → Namespace
─────────────────────────────────────────────────────────────
Common/Http.fs                      → PresenceTwin.Api.Common.Http
Infrastructure/Configuration.fs     → PresenceTwin.Api.Infrastructure.Configuration
Features/Weather/Domain.fs          → PresenceTwin.Api.Features.Weather.Domain
Features/Weather/GetForecast.fs     → PresenceTwin.Api.Features.Weather.GetForecast
```

**Rule**: One namespace per file, namespace = file path

## Core Principles

### 1. Pure Functional Style

✅ **DO:**
```fsharp
type User = { Id: Guid; Name: string }

module User =
    let create id name = { Id = id; Name = name }
    let isValid user = not (String.IsNullOrEmpty user.Name)
```

❌ **DON'T:**
```fsharp
type User(id: Guid, name: string) =
    member this.Id = id
    member this.Name = name
    member this.IsValid() = not (String.IsNullOrEmpty name)
```

### 2. Separate Pure from Impure

✅ **DO:**
```fsharp
// Pure - testable without mocks
let validate (config: Config) (input: Input) : Result<Input, Error> = ...

// Impure - orchestrates with injected dependencies
let execute (deps: Dependencies) (input: Input) : Output =
    let now = deps.GetCurrentTime()  // Impure
    Domain.processData input now     // Pure
```

❌ **DON'T:**
```fsharp
// Mixed - hard to test
let process (input: Input) : Output =
    let now = DateTime.UtcNow        // Hidden dependency!
    Domain.processData input now
```

### 3. Railway-Oriented Programming

✅ **DO:**
```fsharp
type UserError =
    | InvalidEmail of string
    | UserNotFound of Guid

let createUser email =
    email
    |> validateEmail
    |> Result.bind saveToDatabase
    |> Result.map sendWelcomeEmail
```

❌ **DON'T:**
```fsharp
let createUser email =
    try
        if isValidEmail email then
            saveToDatabase email
            sendWelcomeEmail email
        else
            raise (ValidationException "Invalid email")
    with ex -> handleError ex
```

### 4. Dependency Injection via Functions

✅ **DO:**
```fsharp
type GetCurrentTime = unit -> DateTime
type SaveUser = User -> Task<Result<unit, Error>>

type Dependencies = {
    GetCurrentTime: GetCurrentTime
    SaveUser: SaveUser
}

// Composition root
let deps = {
    GetCurrentTime = fun () -> DateTime.UtcNow
    SaveUser = fun user -> Database.saveUser user
}

// Partial application
let handler = CreateUser.httpHandler deps
```

❌ **DON'T:**
```fsharp
type ITimeProvider =
    abstract member GetCurrentTime: unit -> DateTime

type IDatabaseService =
    abstract member SaveUser: User -> Task<Result<unit, Error>>

// Forces OOP patterns
```

## File Templates

### Domain.fs
```fsharp
namespace PresenceTwin.Api.Features.[Feature].Domain

open System

// Types
type [Entity] = {
    Field1: Type1
    Field2: Type2
}

// Module with functions
module [Entity] =
    let create field1 field2 = { Field1 = field1; Field2 = field2 }
    let validate entity = ...

// Business logic
let businessOperation (params: ...) : Result<Output, Error> =
    // Pure implementation
    ...
```

### Query.fs (Read Operation)
```fsharp
namespace PresenceTwin.Api.Features.[Feature].[QueryName]

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Oxpecker
open Oxpecker.OpenApi
open Microsoft.OpenApi.Models
open PresenceTwin.Api.Common.Http
open PresenceTwin.Api.Common.Result
open PresenceTwin.Api.Features.[Feature].Domain

// Query model
type Query = { ... }
type QueryResult = ...
type QueryError = | ...

// Dependencies
type GetCurrentTime = unit -> DateTime
type FetchData = Query -> Task<Data>

type Dependencies = {
    Config: Config
    GetCurrentTime: GetCurrentTime
    FetchData: FetchData
}

// Module
module [QueryName] =
    // Validation (pure)
    let private validate (config: Config) (query: Query) : Result<Query, QueryError> = ...
    
    // Execution (impure - orchestration)
    let private execute (deps: Dependencies) (query: Query) : Task<QueryResult> = ...
    
    // Handler (composition)
    let handle (deps: Dependencies) (query: Query) : Task<Result<QueryResult, QueryError>> = ...
    
    // HTTP endpoint
    let httpHandler (deps: Dependencies) (input: 'T) (ctx: HttpContext) : Task = ...
    
    // OpenAPI config
    let configureOpenApi (endpoint: Endpoint) : Endpoint = ...
```

### Command.fs (Write Operation)
```fsharp
namespace PresenceTwin.Api.Features.[Feature].[CommandName]

// Similar structure to Query.fs but for mutations
type Command = { ... }
type CommandResult = { ... }
type CommandError = | ...

type Dependencies = { ... }

module [CommandName] =
    let private validate ... = ...
    let private execute ... = ...
    let handle ... = ...
    let httpHandler ... = ...
    let configureOpenApi ... = ...
```

### Endpoints.fs
```fsharp
namespace PresenceTwin.Api.Features.[Feature].Endpoints

open System
open Oxpecker
open PresenceTwin.Api.Infrastructure.Configuration

module Endpoints =
    // Dependency creation
    let private createQueryDeps (config: Config) : Query.Dependencies = {
        Config = config
        GetCurrentTime = fun () -> DateTime.UtcNow
        FetchData = fun q -> Database.fetch q
    }
    
    let private createCommandDeps (config: Config) : Command.Dependencies = {
        Config = config
        GetCurrentTime = fun () -> DateTime.UtcNow
        SaveData = fun d -> Database.save d
    }
    
    // Endpoint configuration
    let endpoints (config: Config) : Endpoint list =
        let queryDeps = createQueryDeps config
        let commandDeps = createCommandDeps config
        
        [
            GET [
                routef "/api/[feature]/%s" (fun id ->
                    Query.Query.httpHandler queryDeps id
                )
                |> Query.Query.configureOpenApi
            ]
            
            POST [
                route "/api/[feature]" (Command.Command.httpHandler commandDeps)
                |> Command.Command.configureOpenApi
            ]
        ]
```

## Common Patterns

### 1. Validation with Result
```fsharp
let validateEmail (email: string) : Result<string, ValidationError> =
    if String.IsNullOrEmpty email then
        Error { Field = "email"; Message = "Required" }
    elif not (email.Contains "@") then
        Error { Field = "email"; Message = "Invalid format" }
    else
        Ok email

// Combine validations
let validateUser (user: UserInput) : Result<User, ValidationError> =
    result {
        let! email = validateEmail user.Email
        let! age = validateAge user.Age
        return { Email = email; Age = age }
    }
```

### 2. Async Operations
```fsharp
let fetchUser (id: Guid) : Task<Result<User, Error>> =
    task {
        try
            let! user = Database.getUser id
            return Ok user
        with
        | :? NotFoundException -> return Error (UserNotFound id)
        | ex -> return Error (DatabaseError ex.Message)
    }
```

### 3. Pipeline Composition
```fsharp
let processUser userId =
    userId
    |> validateUserId
    |> Result.bind fetchUser
    |> Result.map enrichUserData
    |> Result.bind saveUser
```

### 4. Error Mapping
```fsharp
let mapErrorToHttp (error: DomainError) (ctx: HttpContext) : Task =
    match error with
    | ValidationError (field, msg) ->
        writeBadRequest "ValidationError" msg (Some {| field = field |}) ctx
    | NotFound id ->
        writeNotFound $"Resource {id} not found" ctx
    | Conflict msg ->
        writeConflict msg ctx
    | _ ->
        writeInternalServerError "An error occurred" ctx
```

## Testing Patterns

### Unit Test (Pure Function)
```fsharp
[<Fact>]
let ``validate should reject invalid email`` () =
    // Arrange
    let input = { Email = "invalid"; Age = 25 }
    
    // Act
    let result = validateUser input
    
    // Assert
    match result with
    | Error { Field = "email" } -> ()
    | _ -> failwith "Expected validation error"
```

### Integration Test (HTTP)
```fsharp
[<Fact>]
let ``POST creates user and returns 201`` () = task {
    // Arrange
    let testDeps = {
        GetCurrentTime = fun () -> DateTime(2025, 1, 1)
        SaveUser = fun u -> Task.FromResult(Ok ())
    }
    
    let request = { Email = "test@test.com"; Age = 25 }
    
    // Act
    let! response = client.PostAsJsonAsync("/api/users", request)
    
    // Assert
    Assert.Equal(HttpStatusCode.Created, response.StatusCode)
}
```

## Anti-Patterns to Avoid

### ❌ 1. Type Extensions
```fsharp
// Bad
type User with
    member this.FullName = $"{this.FirstName} {this.LastName}"
```

### ❌ 2. Mutable State
```fsharp
// Bad
let mutable counter = 0
let increment() = counter <- counter + 1
```

### ❌ 3. Exceptions for Control Flow
```fsharp
// Bad
if isInvalid input then
    raise (ValidationException "Invalid")
```

### ❌ 4. Hidden Dependencies
```fsharp
// Bad
let processOrder order =
    let now = DateTime.UtcNow  // Hidden!
    let random = Random().Next()  // Hidden!
    ...
```

### ❌ 5. Multiple Namespaces per File
```fsharp
// Bad
namespace App.Features.Users
namespace App.Features.Orders  // Different namespace in same file
```

## Checklist for New Features

- [ ] Created folder `Features/[Feature]/`
- [ ] Added `Domain.fs` with pure types and logic
- [ ] Added operation files (Query/Command)
- [ ] Added `Endpoints.fs` for HTTP routing
- [ ] Updated `.fsproj` in bottom-up order
- [ ] Wired endpoints in `Program.fs`
- [ ] Types at namespace level, functions in modules
- [ ] One namespace per file matching file path
- [ ] Pure functions separated from impure
- [ ] Result types for error handling
- [ ] Dependencies injected as functions
- [ ] Added tests (unit + integration)

## Quick Commands

```bash
# Build
dotnet build

# Run
dotnet run --project PresenceTwin.Api

# Test
dotnet test

# Add new file (remember to update .fsproj!)
touch Features/[Feature]/[Operation].fs

# Format code
dotnet fantomas .
```

## Resources

- [F# for Fun and Profit](https://fsharpforfunandprofit.com/)
- [Railway Oriented Programming](https://fsharpforfunandprofit.com/rop/)
- [Dependency Injection in FP](https://fsharpforfunandprofit.com/posts/dependency-injection-1/)
- [Domain Modeling Made Functional](https://pragprog.com/titles/swdddf/domain-modeling-made-functional/)

---

## Common F# Challenges & Solutions

### 1. Pipeline Overuse

**Problem**: Pipelines can become unreadable when overused or too long.

❌ **BAD - Over-pipelined**:
```fsharp
let result =
    input
    |> Option.map validate
    |> Option.map transform
    |> Option.map enrich
    |> Option.bind saveToDb
    |> Option.map notify
    |> Option.defaultValue defaultResult
```

✅ **GOOD - Use pattern matching**:
```fsharp
let result =
    match input with
    | Some value ->
        let validated = validate value
        let transformed = transform validated
        let enriched = enrich transformed
        match saveToDb enriched with
        | Some saved ->
            notify saved
            Some saved
        | None -> None
    | None -> None
```

✅ **BETTER - Use computation expressions**:
```fsharp
let result = option {
    let! value = input
    let validated = validate value
    let transformed = transform validated
    let enriched = enrich transformed
    let! saved = saveToDb enriched
    do! notify saved
    return saved
}
```

**Guidelines**:
- Use pipelines for **2-4 operations max**
- Use pattern matching for **branching logic**
- Use computation expressions for **sequential operations with bind/map**
- Split long operations into **named intermediate steps**

### 2. Long F# Files

**Problem**: F# files grow too large, making them hard to navigate.

**Solutions**:

#### a) Split by Responsibility
```fsharp
// Before: Weather.fs (500 lines)
// After: Split into multiple files
Features/Weather/
├── Domain.fs              (50 lines)  - Types only
├── Validation.fs          (80 lines)  - Pure validation
├── GetForecast.fs        (100 lines)  - Query
├── GenerateForecasts.fs  (100 lines)  - Command
└── Endpoints.fs           (50 lines)  - Routing
```

#### b) Vertical Slices Pattern
Keep each operation in its own file (~100-150 lines max):
- One query/command per file
- Includes types, validation, handler, HTTP endpoint
- Self-contained and easy to understand

#### c) Nested Modules (for related functions)
```fsharp
namespace PresenceTwin.Api.Features.Weather.Domain

type WeatherForecast = { ... }

module WeatherForecast =
    let create ... = ...
    let validate ... = ...
    
    module Calculations =
        let temperatureF ... = ...
        let averageTemp ... = ...
```

**Target**: Keep files under **200 lines** (ideally 100-150)

### 3. .NET BCL / C# Interop

**Problem**: C# APIs don't always map cleanly to F# patterns.

✅ **Wrap C# APIs in F#-friendly functions**:

```fsharp
// Wrap nullable references
let tryParseInt (s: string) : int option =
    match System.Int32.TryParse(s) with
    | true, value -> Some value
    | false, _ -> None

// Wrap exceptions as Results
let tryReadFile (path: string) : Result<string, string> =
    try
        System.IO.File.ReadAllText(path) |> Ok
    with
    | :? System.IO.FileNotFoundException ->
        Error $"File not found: {path}"
    | ex ->
        Error $"Error reading file: {ex.Message}"

// Wrap Task<T> as Task<Result<T, Error>>
let wrapApiCall (apiCall: unit -> Task<'T>) : Task<Result<'T, string>> =
    task {
        try
            let! result = apiCall()
            return Ok result
        with ex ->
            return Error ex.Message
    }
```

**Pattern**: Create a thin F# wrapper module:
```fsharp
// Infrastructure/ExternalApi.fs
module ExternalApi =
    // Wrap third-party C# library
    let fetchData (id: Guid) : Task<Result<Data, Error>> = ...
    let saveData (data: Data) : Task<Result<unit, Error>> = ...
```

### 4. Functions with Many Parameters (DI)

**Problem**: Dependency injection leads to functions with 5+ parameters.

❌ **BAD - Parameter explosion**:
```fsharp
let processOrder 
    (getTime: GetCurrentTime)
    (logger: ILogger)
    (emailSender: SendEmail)
    (validator: ValidateOrder)
    (repository: SaveOrder)
    (order: Order) 
    : Task<Result<OrderId, Error>> = ...
```

✅ **GOOD - Dependencies record**:
```fsharp
type Dependencies = {
    GetCurrentTime: GetCurrentTime
    Logger: ILogger
    SendEmail: SendEmail
    ValidateOrder: ValidateOrder
    SaveOrder: SaveOrder
}

let processOrder (deps: Dependencies) (order: Order) : Task<Result<OrderId, Error>> =
    // Use deps.GetCurrentTime(), deps.Logger, etc.
    ...
```

✅ **BETTER - Group related dependencies**:
```fsharp
type TimeProvider = {
    GetCurrent: unit -> DateTime
    GetUtc: unit -> DateTime
}

type OrderServices = {
    Validate: ValidateOrder
    Save: SaveOrder
}

type Dependencies = {
    Time: TimeProvider
    Logging: ILogger
    Email: SendEmail
    Orders: OrderServices
}

let processOrder (deps: Dependencies) (order: Order) = 
    let now = deps.Time.GetCurrent()
    deps.Orders.Validate order
```

**Pattern**: Never more than **3 parameters** for any function:
1. Dependencies record
2. Input data
3. (Optional) Context (e.g., HttpContext)

### 5. No Real Early Return

**Problem**: F# doesn't have early return, leading to nested code.

❌ **BAD - Deep nesting**:
```fsharp
let processUser user =
    if user.IsActive then
        if user.Email |> String.IsNullOrEmpty |> not then
            if user.Age >= 18 then
                // Process user
                Ok (process user)
            else
                Error "User must be 18+"
        else
            Error "Email required"
    else
        Error "User inactive"
```

✅ **GOOD - Pattern matching (guard clauses)**:
```fsharp
let processUser user =
    match user with
    | { IsActive = false } -> Error "User inactive"
    | { Email = email } when String.IsNullOrEmpty email -> Error "Email required"
    | { Age = age } when age < 18 -> Error "User must be 18+"
    | _ -> Ok (process user)
```

✅ **BETTER - Result computation expression**:
```fsharp
let processUser user = result {
    do! validateActive user
    do! validateEmail user
    do! validateAge user
    return process user
}

// Or with explicit validation
let processUser user =
    result {
        do! if user.IsActive then Ok () else Error "User inactive"
        do! if not (String.IsNullOrEmpty user.Email) then Ok () else Error "Email required"
        do! if user.Age >= 18 then Ok () else Error "User must be 18+"
        return process user
    }
```

✅ **BEST - Validation pipeline**:
```fsharp
module Validation =
    let validateActive user =
        if user.IsActive then Ok user else Error "User inactive"
    
    let validateEmail user =
        if String.IsNullOrEmpty user.Email then Error "Email required" else Ok user
    
    let validateAge user =
        if user.Age >= 18 then Ok user else Error "User must be 18+"

let processUser user =
    user
    |> validateActive
    |> Result.bind validateEmail
    |> Result.bind validateAge
    |> Result.map process
```

**Key**: Use Result/Option for **control flow** instead of nested ifs.

### 6. Debugging Experience

**Problem**: Stepping through pipelines and computation expressions is difficult.

**Solutions**:

#### a) Use `tap` for debugging
```fsharp
module Result =
    let tap (f: 'T -> unit) (result: Result<'T, 'E>) : Result<'T, 'E> =
        match result with
        | Ok value -> 
            f value
            Ok value
        | Error e -> Error e

let processUser user =
    user
    |> validateActive
    |> Result.tap (fun u -> printfn $"After active: {u}")  // Debug point
    |> Result.bind validateEmail
    |> Result.tap (fun u -> printfn $"After email: {u}")   // Debug point
    |> Result.bind validateAge
    |> Result.map process
```

#### b) Break pipelines into named steps
```fsharp
// Instead of one long pipeline
let result =
    input
    |> step1
    |> step2
    |> step3
    |> step4

// Break it up
let afterStep1 = step1 input  // Can set breakpoint here
let afterStep2 = step2 afterStep1
let afterStep3 = step3 afterStep2
let result = step4 afterStep3
```

#### c) Disable "Just My Code" in debugger
- In Rider: Settings → Build, Execution, Deployment → Debugger → Uncheck "Enable 'Just My Code'"
- In VS: Tools → Options → Debugging → General → Uncheck "Enable Just My Code"

#### d) Use explicit returns in computation expressions
```fsharp
// Harder to debug
let process = task {
    let! data = fetchData()
    return! processData data  // Steps into CE machinery
}

// Easier to debug
let process = task {
    let! data = fetchData()
    let! result = processData data
    return result  // Can step through each operation
}
```

### 7. Async vs Task Computation Expressions

**Problem**: When to use `async { }` vs `task { }`?

**Decision Matrix**:

| Scenario | Use | Reason |
|----------|-----|--------|
| ASP.NET Core endpoints | `task { }` | Better interop with Task-based APIs |
| Database calls (EF, Dapper) | `task { }` | Native Task support |
| External HTTP APIs | `task { }` | HttpClient returns Task |
| Pure F# async operations | `async { }` | More composable, cancellation built-in |
| CPU-bound work | Neither | Use synchronous code or Task.Run |
| Mixing async/task | Convert with `Async.AwaitTask` | Keep one type per function |

✅ **RECOMMENDED for this project**: Use `task { }` consistently

```fsharp
// Consistent task usage
let httpHandler (deps: Dependencies) (ctx: HttpContext) : Task =
    task {
        let! request = ctx.BindJson<Request>()
        let! result = processRequest deps request
        return! writeJsonOk result ctx
    }

let processRequest (deps: Dependencies) (request: Request) : Task<Result<Response, Error>> =
    task {
        let validated = validate request  // Sync
        match validated with
        | Ok req ->
            let! data = deps.FetchData req  // Async (Task)
            return Ok (transform data)
        | Error e ->
            return Error e
    }
```

**Avoid mixing**:
```fsharp
// ❌ BAD - Mixing async and task
let process = async {
    let! result = someTaskReturningFunc() |> Async.AwaitTask  // Awkward!
    return result
}

// ✅ GOOD - Consistent task usage
let process = task {
    let! result = someTaskReturningFunc()
    return result
}
```

**Note on `return!`**: 
```fsharp
// These are equivalent:
task {
    let! result = operation()
    return result
}

// Can be simplified to:
task {
    return! operation()
}

// But explicit form is clearer for debugging
```

### Summary: Practical Guidelines

1. **Pipelines**: Keep short (2-4 ops), use CEs for longer flows
2. **File Size**: Target 100-150 lines, max 200 per file
3. **C# Interop**: Wrap in F#-friendly modules with Result types
4. **Dependencies**: Use records, group related deps, max 3 params
5. **Early Return**: Use Result/Option pipelines or pattern matching
6. **Debugging**: Use `tap`, break pipelines, name intermediate steps
7. **Async/Task**: Prefer `task { }` for ASP.NET Core, be consistent

---

**Remember**: 
- **Data** and **Functions** are separate
- **Pure** functions are testable and composable
- **Pipelines** over nested calls **(but keep them short!)**
- **Result** over exceptions
- **Functions** over interfaces
- **Pragmatism** over dogmatism - readability first!

