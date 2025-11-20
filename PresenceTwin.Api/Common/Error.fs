module PresenceTwin.Api.Common.Error

open 

module Http =
    type ErrorResponse =
        { Error: string
          Message: string
          Details: obj option }
        
    module ErrorResponse =
        let createError error message details =
            { Error = error
              Message = message
              Details = details }
