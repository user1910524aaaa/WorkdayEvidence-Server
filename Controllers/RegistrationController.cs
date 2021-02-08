using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Dapper;
using Microsoft.AspNetCore.Authorization;
//using System.Security.Claims;

using Server.Data;
using Server.Services;

namespace Server.Controllers
{
    [ApiController]
    public class RegistrationControl : ControllerBase
    {
        private readonly ISqlDataAccess _db;

        private HashPassword hashPassword = new HashPassword();
        private CustomActionResult customActionResult = new CustomActionResult();

        public RegistrationControl(ISqlDataAccess db)
        {
            _db = db;
        }

        [Authorize]
        [HttpPost("new/api/register")]
        public async Task<IActionResult> Register(UserAccountModel body)
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
                    if(string.IsNullOrEmpty(body.firstName) || string.IsNullOrWhiteSpace(body.firstName))
                    {
                        IActionResult actionResult = customActionResult.FieldsRequired(
                            "The 'firstname' field has been sent from the client null, empty, or whitespaced."
                        );
                        return StatusCode(StatusCodes.Status411LengthRequired, actionResult);
                    }

                    else if(string.IsNullOrEmpty(body.lastName) || string.IsNullOrWhiteSpace(body.lastName))
                    {
                        IActionResult actionResult = customActionResult.FieldsRequired(
                            "The 'firstname' field has been sent from the client null, empty, or whitespaced."
                        );
                        return StatusCode(StatusCodes.Status411LengthRequired, actionResult);
                    }

                    else if(string.IsNullOrEmpty(body.userName) || string.IsNullOrWhiteSpace(body.userName))
                    {
                        IActionResult actionResult = customActionResult.FieldsRequired(
                            "The 'email' field has been sent from the client null, empty, or whitespaced."
                        );
                        return StatusCode(StatusCodes.Status411LengthRequired, actionResult);
                    }

                    else if(string.IsNullOrEmpty(body.password) || string.IsNullOrWhiteSpace(body.password))
                    {
                        IActionResult actionResult = customActionResult.FieldsRequired(
                            "The 'password' field has been sent from the client null, empty, or whitespaced."
                        );
                        return StatusCode(StatusCodes.Status411LengthRequired, actionResult);
                    }

                    else if(string.IsNullOrEmpty(body.role) || string.IsNullOrWhiteSpace(body.role))
                    {
                        IActionResult actionResult = customActionResult.FieldsRequired(
                            "The 'role' field has been sent from the client null, empty, or whitespaced."
                        );
                        return StatusCode(StatusCodes.Status411LengthRequired, actionResult);
                    }

                    else
                    {
                        DynamicParameters selectParameters = new DynamicParameters();
                        selectParameters.Add("@userName", body.userName);
                        string selectQuery = @$"SELECT [id], [userName] FROM [User] WHERE [userName] = @userName";

                        var resultQuery = await _db.SelectAsync<UserAccountModel, dynamic>(selectQuery,  selectParameters);

                        if (resultQuery.Count > 0)
                        {
                            IActionResult actionResult = customActionResult.Conflict(
                                "The user account you are trying to create, already exists."
                            );
                            return StatusCode(StatusCodes.Status409Conflict, actionResult);
                        }

                        else
                        {
                            byte[] salt = hashPassword.CreateSalt(10);
                            string hashedPassword = hashPassword.GenerateSHA256Hash(body.password, salt, false);

                            if(string.IsNullOrEmpty(hashedPassword) || string.IsNullOrWhiteSpace(hashedPassword))
                            {
                                IActionResult actionResult = customActionResult.Locked(
                                    "The password failed to hash, so the account creation process been locked. Please Try Again."
                                );
                                return StatusCode(StatusCodes.Status423Locked, actionResult);
                            }

                            else
                            {
                                DynamicParameters insertParameters = new DynamicParameters();
                                insertParameters.Add("@firstName", body.firstName);
                                insertParameters.Add("@lastName", body.lastName);
                                insertParameters.Add("@userName", body.userName);
                                insertParameters.Add("@role", body.role);
                                insertParameters.Add("@password", hashedPassword);

                                string insertQuery = @"
                                    INSERT INTO [User] ([firstName], [lastName], [userName], [password], [role])
                                    VALUES (@firstName, @lastName, @userName, @password, @role)
                                ";

                                await _db.InsertAsync(insertQuery, insertParameters);

                                IActionResult actionResult = customActionResult.Created(
                                    "The user account as been created successfully. "
                                );
                                return StatusCode(StatusCodes.Status201Created, actionResult);
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
    }
}
