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

namespace Server.Controllers
{
    [ApiController]
    public class GetWorkdaysController : ControllerBase
    {
        private readonly ISqlDataAccess _db;
        private readonly IUserClaims _claims;

        private CustomActionResult customActionResult = new CustomActionResult();

        public GetWorkdaysController(ISqlDataAccess db, IUserClaims claims)
        {
            _db = db;
            _claims = claims;
        }

        [Authorize]
        [HttpGet("new/api/user/workdays")]
        public async Task<IActionResult> Get()
        {
            try
            {
                DynamicParameters selectWorkdaysByUserId_params = new DynamicParameters();
                selectWorkdaysByUserId_params.Add("@userId", _claims.GetUserId());

                string selectWorkdaysByUserId_string = $@"
                    SELECT [id], [date], [startDate], [endDate], [typeCode], [aom], [userId] FROM [Workday] WHERE [userId] = @userId AND [endDate] IS NOT NULL
                ";

                var selectWorkdaysByUserId_result = await _db.SelectAsync<WorkdayModel, dynamic>(
                    selectWorkdaysByUserId_string, selectWorkdaysByUserId_params
                );

                if (selectWorkdaysByUserId_result.Count < 1)
                {
                    IActionResult actionResult = customActionResult.NotFound($"No workdays found in database for this username.");
                    return StatusCode(StatusCodes.Status404NotFound, actionResult);
                }

                else
                {
                    List<WorkdayModel> UserWorkdays = new List<WorkdayModel>();

                    foreach(var workday in selectWorkdaysByUserId_result)
                    {
                        DynamicParameters selectTSignatureByWorkdayId_params = new DynamicParameters();
                        selectTSignatureByWorkdayId_params.Add("@workdayId", workday.id);

                        string selectTSignatureByWorkdayId_string = $@"
                            SELECT TOP 1 [typeCode] FROM [TimeSignature] WHERE [workdayId] = @workdayId
                        ";

                        var selectTSignatureByWorkdayId_result = await _db.SelectAsync<TimeSignatureModel, dynamic>(
                            selectTSignatureByWorkdayId_string, selectTSignatureByWorkdayId_params
                        );

                        UserWorkdays.Add(new WorkdayModel {
                            id = workday.id,
                            date = workday.date,
                            startDate = workday.startDate,
                            endDate = workday.endDate,
                            signatureType = selectTSignatureByWorkdayId_result[0].typeCode,
                            aom = workday.aom,
                            typeCode = workday.typeCode,
                            userId = workday.userId
                        });
                    }

                    IActionResult actionResult = customActionResult.Ok($"Following workdays were found in database for this username.", UserWorkdays);
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
