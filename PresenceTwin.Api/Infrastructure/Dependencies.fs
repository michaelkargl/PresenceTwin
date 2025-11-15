namespace PresenceTwin.Api.Infrastructure.Dependencies

open System

// ==================== TYPE ALIASES ====================

/// Function type for getting current time
type GetCurrentTime = unit -> DateTime

/// Function type for getting random integers
type GetRandomInt = int -> int -> int

module Dependencies =

    // ==================== PRODUCTION IMPLEMENTATIONS ====================
    
    /// Create production time provider (returns current UTC time)
    let createTimeProvider () : GetCurrentTime =
        fun () -> DateTime.UtcNow
    
    /// Create production random provider (uses Random.Shared)
    let createRandomProvider () : GetRandomInt =
        fun min max -> Random.Shared.Next(min, max)
    
    // ==================== TEST IMPLEMENTATIONS ====================
    
    /// Create test time provider with fixed time (for testing)
    let createTestTimeProvider (fixedTime: DateTime) : GetCurrentTime =
        fun () -> fixedTime
    
    /// Create test random provider with predictable sequence (for testing)
    let createTestRandomProvider (values: int seq) : GetRandomInt =
        let enumerator = values.GetEnumerator()
        fun _ _ ->
            if enumerator.MoveNext() then
                enumerator.Current
            else
                0
