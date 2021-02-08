using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Server.Data;
using Server.Models;
using Server.Services;

namespace Server._Controllers
{
    [ApiController]
    public class GetUserByIdController : ControllerBase
    {
        private readonly ISqlDataAccess _db;
        private readonly IUserClaims _claims;

        private CustomActionResult customActionResult = new CustomActionResult();

        public GetUserByIdController(ISqlDataAccess db, IUserClaims claims)
        {
            _db = db;
            _claims = claims;
        }

        [Authorize(Policy = "AdminRolePolicy")]
        [HttpGet("new/api/user/admin/users/uid={id}")]
        public async Task<IActionResult> Get(int id)
        { 
            try
            {
                DynamicParameters selectUsersWorkdaysByUserId_params = new DynamicParameters();
                selectUsersWorkdaysByUserId_params.Add("@userId", id);

                string selectUsersWorkdaysByUserId_string = $@"
                    SELECT [id], [date], [startDate], [endDate], [aom], [typeCode] 
                    FROM [Workday] 
                    WHERE [userId] = @userId
                    AND [endDate] IS NOT NULL
                ";

                var selectUsersWorkdaysByUserId_result = await _db.SelectAsync<WorkdayModel, dynamic>(
                    selectUsersWorkdaysByUserId_string, selectUsersWorkdaysByUserId_params
                );
                if (selectUsersWorkdaysByUserId_result.Count < 1)
                {
                    IActionResult actionResult = customActionResult.NotFound(@$"No workdays found for user id '{id}' in database.");
                    return StatusCode(StatusCodes.Status404NotFound, actionResult);
                }

                else
                {
                    IActionResult actionResult = customActionResult.Ok("User workdays were found in database.", selectUsersWorkdaysByUserId_result);
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
