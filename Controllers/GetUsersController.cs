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

namespace Server._Controllers
{
    [ApiController]
    public class GetUsersController : ControllerBase
    {
        private readonly ISqlDataAccess _db;
        private readonly IUserClaims _claims;

        private CustomActionResult customActionResult = new CustomActionResult();

        public GetUsersController(ISqlDataAccess db, IUserClaims claims)
        {
            _db = db;
            _claims = claims;
        }

        [Authorize(Policy = "AdminRolePolicy")]
        [HttpGet("new/api/user/admin/users")]
        public async Task<IActionResult> Get()
        {
            try
            {
                string selectUsersFromDb_string = $@"SELECT [id], [firstName], [lastName], [userName], [role] FROM [User]";
                var selectUsersFromDb_result = await _db.SelectAsync<UserAccountModel, dynamic>(selectUsersFromDb_string, new UserAccountModel { });

                if (selectUsersFromDb_result.Count < 1)
                {
                    IActionResult actionResult = customActionResult.NotFound("No users found in database.");
                    return StatusCode(StatusCodes.Status404NotFound, actionResult);
                }

                else
                {
                    IActionResult actionResult = customActionResult.Ok("Users found in database.", selectUsersFromDb_result);
                    return StatusCode(StatusCodes.Status200OK, actionResult);
                }
            }

            catch(Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }
    }
}
