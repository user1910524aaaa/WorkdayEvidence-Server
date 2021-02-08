using System;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

using Server.Controllers;
using Server.Data;
using Server.Services;


namespace Server._Controllers
{

    [ApiController]
    public class LoginControl : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly ISqlDataAccess _db;
        private readonly IAuthManager _authManager;

        private CustomActionResult customActionResult = new CustomActionResult();

        public UserAccountModel _user = new UserAccountModel();
        public LoginControl(IConfiguration config, ISqlDataAccess db, IAuthManager authManager)
        {
            _config = config;
            _db = db;
            _authManager = authManager;
        }

        [HttpPost("new/api/login")]
        public async Task<IActionResult> Login(UserAccountModel body)
        {
            try
            {
                if(body == null)
                {
                    IActionResult actionResult = BadRequest(
                        "The client set the requested body to null before it was sent."
                    );
                    return StatusCode(StatusCodes.Status400BadRequest, actionResult);
                }

                else
                {
                    if (string.IsNullOrEmpty(body.userName) || string.IsNullOrWhiteSpace(body.userName))
                    {
                        IActionResult Response = customActionResult.FieldsRequired(
                            "The email field has been sent from the client null, empty, or whitespaced."
                        );
                        return StatusCode(StatusCodes.Status411LengthRequired, Response);
                    }

                    else if(string.IsNullOrEmpty(body.password) || string.IsNullOrWhiteSpace(body.password))
                    {
                        IActionResult Response = customActionResult.FieldsRequired(
                            "The password field has been sent from the client null, empty, or whitespaced."
                        );
                        return StatusCode(StatusCodes.Status411LengthRequired, Response);
                    }

                    else
                    {
                        DynamicParameters userName_params = new DynamicParameters();
                        userName_params.Add("@userName", body.userName);

                        string userName_string = $"SELECT [id], [userName] FROM [User] WHERE [userName] = @userName";
                        var userName_results = await _db.SelectAsync<UserAccountModel, dynamic>(userName_string, userName_params);

                        if(userName_results.Count < 1)
                        {
                            IActionResult actionResult = customActionResult.NotFound(
                                "The username provided does not exist in the database, try again."
                            );
                            return StatusCode(StatusCodes.Status202Accepted, actionResult);
                        }

                        else
                        {
                            _user.userName = userName_results[0].userName;

                            string password_string = $"SELECT [password] FROM [User] WHERE [userName] = @userName";
                            var password_results = await _db.SelectAsync<UserAccountModel, dynamic>(password_string, userName_params);

                            if(password_results.Count < 1)
                            {
                                IActionResult actionResult = customActionResult.NotFound(
                                    "The user details provided do not exist in the database, try again."
                                );
                                return StatusCode(StatusCodes.Status404NotFound, actionResult);
                            }

                            else
                            {
                                _user.password = password_results[0].password;
                                bool comparePassword = new HashPassword().VerifyPassword(_user.password, body.password);

                                if(comparePassword == false)
                                {
                                    IActionResult actionResult = customActionResult.Unauthorized(
                                        "Credentials not accepted, access denied."
                                     );
                                    return StatusCode(StatusCodes.Status401Unauthorized, actionResult);
                                }

                                else
                                {
                                    var token = await Authenticate(body);
                                    IActionResult actionResult = customActionResult.AcceptedUserCredentials(
                                        "User credentials accepted, logged in successfully.", token
                                    );
                                    return StatusCode(StatusCodes.Status202Accepted, actionResult);
                                }
                            }
                        }
                    }
                }
            }

            catch(Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        private async Task<string> Authenticate(UserAccountModel body)
        {
            DynamicParameters userName_params = new DynamicParameters();
            userName_params.Add("@userName", body.userName);

            string claimsString = $"SELECT [id], [role] FROM [User] WHERE [userName] = @userName";
            var claimsList = await _db.SelectAsync<UserAccountModel, dynamic>(claimsString, userName_params);

            if(claimsList.Count < 1)
            {
                IActionResult actionResult = customActionResult.UnavailableForLegalReasons(
                    "Claims unavailable for the current user. Authorization denided."
                );
                StatusCode(StatusCodes.Status451UnavailableForLegalReasons, actionResult);
                return null;
            }

            else
            {
                _user.id = claimsList[0].id;
                _user.role = claimsList[0].role;
                return _authManager.GenerateJSONWebToken(body, _user);
            }
        }
    }
}
