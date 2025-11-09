namespace PresenceTwin.Api.Infrastructure

open System

module Dependencies =
    
    /// Time provider abstraction (for testing)
    type ITimeProvider = {
        GetCurrentTime: unit -> DateTime
    }
    
    /// Random number generator abstraction (for testing)
    type IRandomProvider = {
        GetInt: int -> int -> int
    }
    
    /// Create production time provider
    let createTimeProvider () : ITimeProvider =
        { GetCurrentTime = fun () -> DateTime.UtcNow }
    
    /// Create production random provider
    let createRandomProvider () : IRandomProvider =
        { GetInt = fun min max -> Random.Shared.Next(min, max) }
    
    /// Create test time provider with fixed time
    let createTestTimeProvider (fixedTime: DateTime) : ITimeProvider =
        { GetCurrentTime = fun () -> fixedTime }
    
    /// Create test random provider with predictable sequence
    let createTestRandomProvider (values: int seq) : IRandomProvider =
        let enumerator = values.GetEnumerator()
        { GetInt = fun _ _ ->
            if enumerator.MoveNext() then
                enumerator.Current
            else
                0
        }
