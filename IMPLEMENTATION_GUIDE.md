# Complete CQRS Vertical Slices Implementation

## Step-by-Step Implementation

This guide shows the complete refactoring from the current structure to CQRS with vertical slices.

## Project Structure

```
PresenceTwin.Api/
├── Program.fs
├── Common/
│   ├── Result.fs
│   ├── Validation.fs
│   └── Http.fs
├── Infrastructure/
│   ├── Configuration.fs
│   └── Dependencies.fs
└── Features/
    └── Weather/
        ├── Domain.fs
        ├── GetForecast.fs          # Query
        ├── GenerateForecasts.fs    # Command
        └── Endpoints.fs
```

## Implementation Order

1. Create Common modules
2. Create Infrastructure modules
3. Create Weather feature (Domain first)
4. Implement Query (GetForecast)
5. Implement Command (GenerateForecasts)
6. Create Endpoints
7. Update Program.fs
8. Update .fsproj file

## File Compilation Order (Important for F#!)

The .fsproj file MUST list files in dependency order:
```xml
<ItemGroup>
  <!-- Common (no dependencies) -->
  <Compile Include="Common/Result.fs" />
  <Compile Include="Common/Validation.fs" />
  <Compile Include="Common/Http.fs" />
  
  <!-- Infrastructure (depends on Common) -->
  <Compile Include="Infrastructure/Configuration.fs" />
  <Compile Include="Infrastructure/Dependencies.fs" />
  
  <!-- Features (depends on Common and Infrastructure) -->
  <Compile Include="Features/Weather/Domain.fs" />
  <Compile Include="Features/Weather/GetForecast.fs" />
  <Compile Include="Features/Weather/GenerateForecasts.fs" />
  <Compile Include="Features/Weather/Endpoints.fs" />
  
  <!-- Entry point (depends on everything) -->
  <Compile Include="Program.fs" />
</ItemGroup>
```

## Key Enterprise Patterns

### 1. Dependency Injection (Functional Style)
- Pass functions as dependencies
- Use record types for dependency groups
- Partial application for handler creation

### 2. Validation
- Validate early, fail fast
- Return detailed error messages
- Use Result type for all validations

### 3. Error Handling
- Domain errors (business rule violations)
- Infrastructure errors (database, network)
- Map errors to HTTP responses

### 4. Logging (Cross-cutting)
- Structured logging
- Correlation IDs
- Performance metrics

### 5. Testing
- Unit tests for domain logic (pure functions)
- Integration tests for handlers
- Contract tests for HTTP endpoints

## Differences from Traditional C# CQRS

### C# (MediatR pattern)
```csharp
public class CreateForecastCommand : IRequest<CreateForecastResult>
{
    public int Count { get; set; }
}

public class CreateForecastHandler : IRequestHandler<CreateForecastCommand, CreateForecastResult>
{
    private readonly IWeatherRepository _repository;
    
    public CreateForecastHandler(IWeatherRepository repository)
    {
        _repository = repository;
    }
    
    public async Task<CreateForecastResult> Handle(CreateForecastCommand request, CancellationToken ct)
    {
        // Implementation
    }
}
```

### F# (Functional pattern)
```fsharp
// Command is just a type
type GenerateForecastsCommand = { Count: int }

// Dependencies are functions
type Dependencies = {
    GetCurrentTime: unit -> DateTime
    GenerateRandom: int -> int -> int
}

// Handler is just a function
let handle (deps: Dependencies) (command: GenerateForecastsCommand) =
    // Implementation
```

## Benefits of F# Approach

1. **Simpler**: No interfaces, no classes, just functions
2. **Explicit**: Dependencies are visible in type signature
3. **Testable**: Easy to mock with record updates
4. **Composable**: Functions compose naturally
5. **Type-safe**: Compiler enforces contracts

## Next Steps

Follow the individual file implementations in the next documents.

