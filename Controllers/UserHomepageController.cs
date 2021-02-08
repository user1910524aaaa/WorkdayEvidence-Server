using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

using Server.Services;
using Server.Data;
using Dapper;

namespace Server._Controllers
{
    [ApiController]
    public class UserHomepageController : ControllerBase
    {
        private readonly ISqlDataAccess _db;
        private readonly IUserClaims _claims;

        private CustomActionResult customActionResult = new CustomActionResult();

        public UserHomepageController(ISqlDataAccess db, IUserClaims claims)
        {
            _db = db;
            _claims = claims;
        }

        [Authorize]
        [HttpGet("new/api/user")]
        public async Task<IActionResult> GetUserProfile()
        { 
            try
            {
                DynamicParameters getUserProfile_params = new DynamicParameters();
                getUserProfile_params.Add("@userId", _claims.GetUserId());

                string getUserProfile_string = @$"
                    SELECT [id], [firstName], [lastName], [userName], [role]  FROM [User] WHERE [id] = @userId
                ";

                var userProfile = await _db.SelectAsync<UserAccountModel, dynamic>(
                    getUserProfile_string, getUserProfile_params
                );

                IActionResult actionResult = customActionResult.Ok(
                    "User profile loaded", userProfile
                );
                return StatusCode(StatusCodes.Status200OK, actionResult);


            }
            catch(Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }
    }
}
