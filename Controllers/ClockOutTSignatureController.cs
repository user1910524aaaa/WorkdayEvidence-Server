using System;
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
    public class ClockOutEndWorkdayController : ControllerBase
    {
        private readonly ISqlDataAccess _db;
        private readonly IUserClaims _claims;

        private CustomActionResult customActionResult = new CustomActionResult();

        public ClockOutEndWorkdayController(ISqlDataAccess db, IUserClaims claims)
        {
            _db = db;
            _claims = claims;
        }

        [Authorize]
        [HttpPost("new/api/user/clockOut")]
        public async Task<IActionResult> ClockOut(TimeSignatureModel body)
        {
            try
            {
                if (body == null)
                {
                    IActionResult actionResult = customActionResult.BadRequest(
                        "The client set the requested body to null before it was sent."
                    );
                    return StatusCode(StatusCodes.Status400BadRequest, actionResult);
                }

                else
                {

                    if (body.finish == false)
                    {
                        DynamicParameters checkIfClockOut_params = new DynamicParameters();
                        checkIfClockOut_params.Add("@userId", _claims.GetUserId());

                        string checkIfClockOut_string = @$"
                            SELECT TOP 1 [id], [startDate] FROM [TimeSignature] WHERE [endDate] IS NULL AND [userId] = @userId ORDER BY [id] DESC
                        ";

                        var checkIfClockOut_results = await _db.SelectAsync<TimeSignatureModel, dynamic>(
                            checkIfClockOut_string, checkIfClockOut_params
                        );

                        if (checkIfClockOut_results.Count < 1)
                        {
                            IActionResult actionResult = customActionResult.Locked(
                                @$"You already clocked out. You must clock in before clock out again."
                            );
                            return StatusCode(StatusCodes.Status423Locked, actionResult);
                        }

                        else
                        {
                            DynamicParameters clockOut_params = new DynamicParameters();
                            clockOut_params.Add("@startDate", checkIfClockOut_results[0].startDate);
                            clockOut_params.Add("@setState", body.stateCode);
                            clockOut_params.Add("@id", checkIfClockOut_results[0].id);
                            clockOut_params.Add("@userId", _claims.GetUserId());


                            string clockOut_string = $@"
                                UPDATE 
                                    [TimeSignature] 
                                SET
                                    [endDate]=(SELECT  GETDATE()),
                                    [aom]=(SELECT DATEDIFF(MINUTE, @startDate, (SELECT GETDATE())))
                                WHERE [userId] = @userId AND [id] = @id
                            ";

                            await _db.UpdateAsync(clockOut_string, clockOut_params);

                            IActionResult actionResult = customActionResult.Ok(
                                "You have clocked out, your work time is recorded to database."
                            );
                            return StatusCode(StatusCodes.Status200OK, actionResult);
                        }
                    }

                    else if (body.finish == true)
                    {
                        DynamicParameters checkIfClockOut_params = new DynamicParameters();
                        checkIfClockOut_params.Add("@userId", _claims.GetUserId());

                        string checkIfClockOut_string = @$"
                            SELECT TOP 1 [id], [startDate] FROM [TimeSignature] WHERE [endDate] IS NULL AND [userId] = @userId ORDER BY [id] DESC
                        ";

                        var clockOut_results = await _db.SelectAsync<TimeSignatureModel, dynamic>(
                            checkIfClockOut_string, checkIfClockOut_params
                        );

                        if (clockOut_results.Count < 1)
                        {
                            IActionResult actionResult = customActionResult.Locked(
                                @$"You already clocked out. You must clock in before clock out again."
                            );
                            return StatusCode(StatusCodes.Status423Locked, actionResult);
                        }

                        else
                        {
                            DynamicParameters selectUnfinishedWorkdayTSignature_params = new DynamicParameters();
                            selectUnfinishedWorkdayTSignature_params.Add("@userId", _claims.GetUserId());

                            string selectUnfinishedWorkdayTSignature_string = @$"
                                SELECT [id], [endDate], [stateCode], [aom], [workdayId] FROM [TimeSignature] WHERE [stateCode] = 'IP' AND [userId] = @userId
                            ";

                            var selectUnfinishedWorkdayTSignature = await _db.SelectAsync<TimeSignatureModel, dynamic>(
                                selectUnfinishedWorkdayTSignature_string, selectUnfinishedWorkdayTSignature_params
                             );

                            if (selectUnfinishedWorkdayTSignature.Count < 1)
                            {
                                IActionResult actionResult = customActionResult.NotFound(
                                    @$"No unfinished time signatures found for today."
                                );
                                return StatusCode(StatusCodes.Status404NotFound, actionResult);
                            }

                            else
                            {
                                DynamicParameters updateCurrentTsignatureAom_params = new DynamicParameters();
                                updateCurrentTsignatureAom_params.Add("@startDate", clockOut_results[0].startDate);
                                updateCurrentTsignatureAom_params.Add("@stateCode", body.stateCode);
                                updateCurrentTsignatureAom_params.Add("@id", clockOut_results[0].id);

                                string updateCurrentTSignatureAom_string = $@"
                                    UPDATE 
                                        [TimeSignature] 
                                    SET
                                        [endDate] = (SELECT GETDATE()),
                                        [aom] = (SELECT DATEDIFF(MINUTE, @startDate, (SELECT GETDATE())))
                                    WHERE [id] = @id
                                ";

                                await _db.UpdateAsync(updateCurrentTSignatureAom_string, updateCurrentTsignatureAom_params);

                                DynamicParameters updateWorkday_params = new DynamicParameters();
                                updateWorkday_params.Add("@userId", _claims.GetUserId());
                                updateWorkday_params.Add("@workdayId", selectUnfinishedWorkdayTSignature[0].workdayId);

                                string updateWorkday_string = @$"
                                    UPDATE [Workday] 
                                    SET 
                                        [aom] = (SELECT SUM([aom]) FROM [TimeSignature] WHERE [userId] = @userId AND [workdayId] = @workdayId),
                                        [endDate] = (SELECT GETDATE())
                                    WHERE 
                                        [userId] = @userId 
                                    AND [id] = @workdayId
                                ";

                                await _db.UpdateAsync(updateWorkday_string, updateWorkday_params);
                                
                                DateTime dateNow = new DateTime();
                                dateNow = DateTime.Now.Date;

                                foreach (var item in selectUnfinishedWorkdayTSignature)
                                {
                                    if (item.endDate.Date == dateNow)
                                    {
                                        DynamicParameters updateUnfinishedWorkdayTSignatureState_params = new DynamicParameters();
                                        updateUnfinishedWorkdayTSignatureState_params.Add("@startDate", clockOut_results[0].startDate);
                                        updateUnfinishedWorkdayTSignatureState_params.Add("@stateCode", body.stateCode);
                                        updateUnfinishedWorkdayTSignatureState_params.Add("@id", item.id);


                                        string updateUnfinishedWorkdayTSignatureState_string = $@"
                                            UPDATE 
                                                [TimeSignature] 
                                            SET
                                                [stateCode] = (SELECT [code] FROM [SignatureState] WHERE [code] = 'TT')
                                            WHERE [id] = @id
                                        ";

                                        await _db.UpdateAsync(updateUnfinishedWorkdayTSignatureState_string, updateUnfinishedWorkdayTSignatureState_params);
                                    }
                                }

                                DynamicParameters clockOut_params = new DynamicParameters();
                                clockOut_params.Add("@userId", _claims.GetUserId());

                                string updateCurrentTSignatureState_string = $@"
                                    UPDATE 
                                        [TimeSignature] 
                                    SET
                                        [stateCode] = (SELECT [code] FROM [SignatureState] WHERE [code] = 'TT')
                                    WHERE [userId] = @userId AND [stateCode] = 'IP'
                                ";

                                await _db.UpdateAsync(updateCurrentTSignatureState_string, clockOut_params);

                                IActionResult actionResult = customActionResult.Ok(
                                    "You have clocked out and finished your workday, your current signature and current workday were succesfully recorded into database."
                                );
                                return StatusCode(StatusCodes.Status200OK, actionResult);
                            }
                        }
                    }

                    else
                    {
                        IActionResult actionResult = customActionResult.Locked(
                            @$"'State code' provided by the client is invalid. You're unable to clock out safe.
                            'State code' provided by client: {body.stateCode}"
                        );
                        return StatusCode(StatusCodes.Status423Locked, actionResult);
                    }
                }
            }

            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }


        }
    }
}
