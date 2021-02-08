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
    public class ClockInFlxTSignatureState : ControllerBase
    {
        private readonly ISqlDataAccess _db;
        private readonly IUserClaims _claims;

        public string _state = "";

        private CustomActionResult customActionResult = new CustomActionResult();

        public ClockInFlxTSignatureState(ISqlDataAccess db, IUserClaims claims)
        {
            _db = db;
            _claims = claims;
        }

        [Authorize]
        [HttpGet("new/api/user/clockIn/flxTSignature/state")]
        public async Task<IActionResult> VerifyState()
        {
            try
            {
                var checkIfAnyFinishedTSignatureSavedInDbWithCurrentDate = await CheckIfAnyFinishedTSignatureSavedInDbWithCurrentDate();

                if (
                    checkIfAnyFinishedTSignatureSavedInDbWithCurrentDate.Count > 0 &&
                    checkIfAnyFinishedTSignatureSavedInDbWithCurrentDate[0].typeCode == "FLX" &&
                    checkIfAnyFinishedTSignatureSavedInDbWithCurrentDate[0].stateCode == "TT" &&
                    checkIfAnyFinishedTSignatureSavedInDbWithCurrentDate[0].endDate.Date == DateTime.Now.Date
                   )
                {
                    IActionResult actionResult = customActionResult.Locked(
                        @$"You already finished your workday for today using flexible time signatures. If you checked in the finish checkbox from the client by mistake, contact your manager. Have a nice day and see you tommorrow",
                        _state = "TT"
                    );
                    return StatusCode(StatusCodes.Status423Locked, actionResult);
                }

                else
                {
                    var checkIfAnyStartedTSignatureSavedInDbWithCurrentDate = await CheckIfAnyStartedTSignatureSavedInDbWithCurrentDate();

                    if (
                        checkIfAnyStartedTSignatureSavedInDbWithCurrentDate.Count > 0 &&
                        checkIfAnyStartedTSignatureSavedInDbWithCurrentDate[0].stateCode == "IP"
                       )
                    {
                        IActionResult actionResult = customActionResult.Locked(
                            @$"You already clocked in. You must clock out before you clock in again. Your session already started at {checkIfAnyStartedTSignatureSavedInDbWithCurrentDate[0].startDate}",
                            _state = "IP"
                        );
                        return StatusCode(StatusCodes.Status423Locked, actionResult);
                    }

                    else
                    {
                        IActionResult actionResult = customActionResult.Ok(
                            @$"You are free to clock in.", _state = "OK"
                        );
                        return StatusCode(StatusCodes.Status200OK, actionResult);
                    }
                }
            }

            catch(Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        private async Task<List<TimeSignatureModel>> CheckIfAnyFinishedTSignatureSavedInDbWithCurrentDate()
        {
            DynamicParameters checkIfAnyTSignatureSavedInDbWithCurrentDate_params = new DynamicParameters();
            checkIfAnyTSignatureSavedInDbWithCurrentDate_params.Add("@userId", _claims.GetUserId());

            string checkIfAnyTSignatureSavedInDbWithCurrentDate_string = @$"
                SELECT TOP 1 [id], [endDate], [typeCode], [stateCode] FROM [TimeSignature] WHERE [endDate] IS NOT NULL AND [userId] = @userId ORDER BY [id] DESC
            ";

            var checkIfAnyTSignatureSavedInDbWithCurrentDate_results = await _db.SelectAsync<TimeSignatureModel, dynamic>(
               checkIfAnyTSignatureSavedInDbWithCurrentDate_string, checkIfAnyTSignatureSavedInDbWithCurrentDate_params
            );

            return checkIfAnyTSignatureSavedInDbWithCurrentDate_results;
        }

        private async Task<List<TimeSignatureModel>> CheckIfAnyStartedTSignatureSavedInDbWithCurrentDate()
        {
            DynamicParameters checkIfClockInFlx_params = new DynamicParameters();
            checkIfClockInFlx_params.Add("@userId", _claims.GetUserId());

            string checkIfClockInFlx_string = @$"
                SELECT TOP 1 [id], [startDate], [stateCode] FROM [TimeSignature] WHERE [endDate] IS NULL AND [userId] = @userId ORDER BY [id] DESC
            ";

            var checkIfClockInFlx_results = await _db.SelectAsync<TimeSignatureModel, dynamic>(
                checkIfClockInFlx_string, checkIfClockInFlx_params
            );

            return checkIfClockInFlx_results;
        }
    }
}
