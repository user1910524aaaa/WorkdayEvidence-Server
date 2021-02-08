using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

using Server.Data;
using Server.Models;
using Server.Services;

namespace Server.Controllers
{
    [ApiController]
    public class GetWorkdayTypeController : ControllerBase
    {
        private readonly ISqlDataAccess _db;

        private readonly IUserClaims _claims;
        private CustomActionResult customActionResult = new CustomActionResult();

        public GetWorkdayTypeController(ISqlDataAccess db, IUserClaims claims)
        {
            _db = db;
            _claims = claims;
        }

        [Authorize]
        [HttpGet("new/api/user/clockIn/workdayType")]
        public async Task<IActionResult> WorkdayType()
        {
            try
            {
                string workdayTypeFixString = @$"SELECT [id], [code], [name] FROM [WorkdayType]";

                var workdayTypeDBResults = await _db.SelectAsync<GetWorkdayTypeModel, dynamic>(
                    workdayTypeFixString, new GetWorkdayTypeModel { }
                );

                if (workdayTypeDBResults.Count < 1)
                {
                    IActionResult actionResult = customActionResult.NotFound(@$"No 'Workday Type' elements found in database.");
                    return StatusCode(StatusCodes.Status404NotFound, actionResult);
                }

                else
                {
                    IActionResult actionResult = customActionResult.Ok(@$"Workday types found.", workdayTypeDBResults);
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
