namespace PresenceTwin.Api.Infrastructure.Dependencies

open System


type GetCurrentTime = unit -> DateTime
type GetRandomInt = int -> int -> int

module Dependencies =
    let createTimeProvider () : GetCurrentTime =
        fun () -> DateTime.UtcNow
    
    let createRandomProvider () : GetRandomInt =
        fun min max -> Random.Shared.Next(min, max)
    
    let createTestTimeProvider (fixedTime: DateTime) : GetCurrentTime =
        fun () -> fixedTime
    
    let createTestRandomProvider (values: int seq) : GetRandomInt =
        let enumerator = values.GetEnumerator()
        fun _ _ ->
            if enumerator.MoveNext() then
                enumerator.Current
            else
                0
