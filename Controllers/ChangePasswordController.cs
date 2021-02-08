using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Server.Data;
using Server.Services;

namespace Server.Controllers
{
    [ApiController]
    public class ChangePasswordController : ControllerBase
    {
        private readonly ISqlDataAccess _db;

        private readonly IUserClaims _claims;

        private HashPassword hashPassword = new HashPassword();
        private CustomActionResult customActionResult = new CustomActionResult();

        public UserAccountModel _user = new UserAccountModel();

        public ChangePasswordController(ISqlDataAccess db, IUserClaims claims)
        {
            _db = db;
            _claims = claims;
        }

        [Authorize]
        [HttpPost("new/api/user/settings/changePassword")]
        public async Task<IActionResult> ChangePassword(UserAccountModel body)
        {
            try
            {
                if (body == null)
                {
                    IActionResult actionResult = BadRequest(
                        "The client set the requested body to null before it was sent."
                    );
                    return StatusCode(StatusCodes.Status400BadRequest, actionResult);
                }

                else if (string.IsNullOrEmpty(body.password) || string.IsNullOrWhiteSpace(body.password))
                {
                    IActionResult actionResult = customActionResult.FieldsRequired(
                        "The 'password' field has been sent from the client null, empty, or whitespaced."
                    );
                    return StatusCode(StatusCodes.Status411LengthRequired, actionResult);
                }

                else if (string.IsNullOrEmpty(body.oldPassword) || string.IsNullOrWhiteSpace(body.oldPassword))
                {
                    IActionResult actionResult = customActionResult.FieldsRequired(
                        "The 'old password' field has been sent from the client null, empty, or whitespaced."
                    );
                    return StatusCode(StatusCodes.Status411LengthRequired, actionResult);
                }

                else
                {
                    DynamicParameters selectPasswordByUserId_params = new DynamicParameters();
                    selectPasswordByUserId_params.Add("@userId", _claims.GetUserId());

                    string selectPasswordByUserId_string = $"SELECT [password] FROM [User] WHERE [id] = @userId";
                    var selectPasswordByUserId_results = await _db.SelectAsync<UserAccountModel, dynamic>(selectPasswordByUserId_string, selectPasswordByUserId_params);

                    if (selectPasswordByUserId_results.Count < 1)
                    {
                        IActionResult actionResult = customActionResult.NotFound(
                            "The user details provided do not exist in the database, try again."
                        );
                        return StatusCode(StatusCodes.Status404NotFound, actionResult);
                    }

                    else
                    {
                        _user.oldPassword = selectPasswordByUserId_results[0].password;
                        bool comparePassword = new HashPassword().VerifyPassword(_user.oldPassword, body.oldPassword);

                        byte[] salt = hashPassword.CreateSalt(10);

                        string newPasswordHashed = hashPassword.GenerateSHA256Hash(body.password, salt, false);

                        if (comparePassword != true)
                        {
                            IActionResult actionResult = customActionResult.Unauthorized(
                               "The old password is wrong, so the access is denied."
                            );
                            return StatusCode(StatusCodes.Status401Unauthorized, actionResult);
                        }

                        else if (string.IsNullOrEmpty(newPasswordHashed) || string.IsNullOrWhiteSpace(newPasswordHashed))
                        {
                            IActionResult actionResult = customActionResult.Locked(
                                "The new password failed to hash, so the process to change the password has been blocked. Please Try Again."
                            );
                            return StatusCode(StatusCodes.Status423Locked, actionResult);
                        }

                        else
                        {
                            DynamicParameters updatePassword_params = new DynamicParameters();
                            updatePassword_params.Add("@userId", _claims.GetUserId());
                            updatePassword_params.Add("@newPassword", newPasswordHashed);

                            string updatePassword_string = @"UPDATE [User] SET [password] = @newPassword WHERE [id] = @userId";

                            await _db.UpdateAsync(updatePassword_string, updatePassword_params);

                            IActionResult actionResult = customActionResult.Ok(
                                "Password changed successfully. Be careful and don't give it to anyone."
                            );
                            return StatusCode(StatusCodes.Status202Accepted, actionResult);
                        }
                    }
                }
            }

            catch(Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }
    }
}
