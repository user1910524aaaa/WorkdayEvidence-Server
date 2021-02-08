using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace Server.Data
{

    public class CustomActionResult: ActionResult
    {

        public string type { get; set; }

        public bool success { get; set; }

        public string message { get; set; }

        public int statusCode { get; set; }

        public string token { get; set; }

        public dynamic data { get; set; }


        // **********************************

        // STANDARD CUSTOMIZED HTTP RESPONSES

        // **********************************


        // STATUS CODE 100
        public ActionResult Continue(string message, dynamic data = null)
        { 

            return new CustomActionResult {
                type = "informational",
                success = true,
                statusCode = StatusCodes.Status100Continue,
                message = message,
                data = data
            };
            
        }

        // STATUS CODE 200
        public ActionResult Ok(string message, dynamic data = null)
        {
            
            return new CustomActionResult {
                type = "success",
                success = true,
                statusCode = StatusCodes.Status200OK,
                message = message,
                data = data
            };
            
        }

        public ActionResult Created(string message, dynamic data = null)
        { 

            return new CustomActionResult {
                type = "success",
                success = true,
                statusCode = StatusCodes.Status201Created,
                message = message,
                data = data
            };
            
        }

        public ActionResult Accepted(string message, dynamic data = null)
        { 

            return new CustomActionResult {
                type = "success",
                success = true,
                statusCode = StatusCodes.Status202Accepted,
                message = message,
                data = data
            };
            
        }

        public ActionResult NoContent(string message, dynamic data = null)
        { 

            return new CustomActionResult {
                type = "success",
                success = true,
                statusCode = StatusCodes.Status204NoContent,
                message = message,
                data = data
            };
            
        }

        public ActionResult PartialContent(string message, dynamic data = null)
        { 

            return new CustomActionResult {
                type = "success",
                success = true,
                statusCode = StatusCodes.Status206PartialContent,
                message = message,
                data = data
            };
 
        }

        // STATUS CODE 300
        public ActionResult MovedPermanently(string message, dynamic data = null)
        { 

            return new CustomActionResult {
                type = "error",
                success = false,
                statusCode = StatusCodes.Status301MovedPermanently,
                message = message,
                data = data
            };
            
        }

        public ActionResult Found(string message, dynamic data = null)
        { 

            return new CustomActionResult {
                type = "success",
                success = true,
                statusCode = StatusCodes.Status302Found,
                message = message,
                data = data
            };
            
        }

        // STATUS CODE 400

        public ActionResult BadRequest(string message, dynamic data = null)
        {

            return new CustomActionResult {
                type = "error",
                success = false,
                statusCode = StatusCodes.Status400BadRequest,
                message = message,
                data = data
            };
        }

        public ActionResult Unauthorized(string message, dynamic data = null)
        { 

            return new CustomActionResult {
                type = "error",
                success = false,
                statusCode = StatusCodes.Status401Unauthorized,
                message = message,
                data = data
            };

        }

        public ActionResult Forbidden(string message, dynamic data = null)
        { 

            return new CustomActionResult {
                type = "error",
                success = false,
                statusCode = StatusCodes.Status403Forbidden,
                message = message,
                data = data
            };
            
        }

        public ActionResult NotFound(string message, dynamic data = null)
        { 

            return new CustomActionResult {
                type = "error",
                success = false,
                statusCode = StatusCodes.Status404NotFound,
                message = message,
                data = data
            };
            
        }

        public ActionResult Conflict(string message, dynamic data = null)
        { 

            return new CustomActionResult {
                type = "error",
                success = false,
                statusCode = StatusCodes.Status409Conflict,
                message = message,
                data = data
            };
            
        }

        public ActionResult Locked(string message, dynamic data = null)
        { 

            return new CustomActionResult {
                type = "error",
                success = false,
                statusCode = StatusCodes.Status423Locked,
                message = message,
                data = data
            };
            
        }

        public ActionResult FailedDependency(string message, dynamic data = null)
        { 

            return new CustomActionResult {
                type = "error",
                success = false,
                statusCode = StatusCodes.Status424FailedDependency,
                message = message,
                data = data
            };
            
        }

        public ActionResult UnavailableForLegalReasons(string message, dynamic data = null)
        {

            return new CustomActionResult
            {
                type = "error",
                success = false,
                statusCode = StatusCodes.Status451UnavailableForLegalReasons,
                message = message,
                data = data
            };

        }

        public ActionResult NotAccepted(string message, dynamic data = null)
        { 

            return new CustomActionResult {
                type = "error",
                success = false,
                statusCode = StatusCodes.Status406NotAcceptable,
                message = message,
                data = data
            };
            
        }

        public ActionResult FieldsRequired(string message, dynamic data = null)
        { 

            return new CustomActionResult {
                type = "error",
                success = false,
                statusCode = StatusCodes.Status411LengthRequired,
                message = message,
                data = data
            };
            
        }

        // STATUS CODE 500
        public ActionResult InternalServerError(string message, dynamic data = null)
        { 

            return new CustomActionResult {
                type = "error",
                success = false,
                statusCode = StatusCodes.Status500InternalServerError,
                message = message,
                data = data
            };
            
        }


        // *******************************

        // EXTRA CUSTOMIZED HTTP RESPONSES

        // *******************************
        

        // STATUS CODE 200
        public ActionResult AcceptedUserCredentials(string message, string token, dynamic data = null)
        { 

            return new CustomActionResult {
                type = "success",
                success = true,
                statusCode = StatusCodes.Status202Accepted,
                message = message,
                data = data,
                token = token
            };
            
        }

    }

}
