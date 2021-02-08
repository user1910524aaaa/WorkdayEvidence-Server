using System;
using System.Collections.Generic;
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
    public class ClockInFlxTSignatureController : ControllerBase
    {
        private readonly ISqlDataAccess _db;
        private readonly IUserClaims _claims;

        private CustomActionResult customActionResult = new CustomActionResult();

        public ClockInFlxTSignatureController(ISqlDataAccess db, IUserClaims claims)
        {
            _db = db;
            _claims = claims;
        }
        
        [Authorize]
        [HttpPost("new/api/user/clockIn/flxTSignature")]
        public async Task<IActionResult> ClockIn(TimeSignatureModel body)
        { 
            try
            { 
                if (body == null)
                {
                    IActionResult actionResult = customActionResult.BadRequest("The client set the requested body to null before it was sent.");
                    return StatusCode(StatusCodes.Status400BadRequest, actionResult);
                }

                else if (string.IsNullOrEmpty(body.typeCode) || string.IsNullOrWhiteSpace(body.typeCode))
                {
                    IActionResult actionResult = customActionResult.FieldsRequired(
                        "The 'type code' field has been sent from the client null, empty, or whitespaced."
                    );
                    return StatusCode(StatusCodes.Status411LengthRequired, actionResult);
                }

                else if (body.typeCode != "FLX")
                {
                    IActionResult actionResult = customActionResult.Conflict(
                        "This option is only for 'Fixed Time Signature'. For 'Flexible Time Signature', try the other option."
                    );
                    return StatusCode(StatusCodes.Status409Conflict, actionResult);
                }

                else if (string.IsNullOrEmpty(body.wd_typeCode) || string.IsNullOrWhiteSpace(body.wd_typeCode))
                {
                    IActionResult actionResult = customActionResult.FieldsRequired(
                        "The Workday 'code type' field has been sent from the client null, empty, or whitespaced."
                    );
                    return StatusCode(StatusCodes.Status411LengthRequired, actionResult);
                }

                else
                {
                    var checkIfAnyWorkdaySavedInDbWithCurrentDate = await CheckIfAnyWorkdaySavedInDbWithCurrentDate();

                    if 
                    (
                       checkIfAnyWorkdaySavedInDbWithCurrentDate.Count > 0 &&
                       checkIfAnyWorkdaySavedInDbWithCurrentDate[0].typeCode == "FLX" &&
                       checkIfAnyWorkdaySavedInDbWithCurrentDate[0].stateCode == "TT" &&
                       checkIfAnyWorkdaySavedInDbWithCurrentDate[0].endDate.Date == DateTime.Now.Date
                    )
                    {
                        IActionResult actionResult = customActionResult.Locked(
                            @$"You already finished your workday for today using flexible time signatures. If you checked in the finish checkbox from the client by mistake, contact your manager. Have a nice day and see you tommorrow"
                        );
                        return StatusCode(StatusCodes.Status423Locked, actionResult);
                    }

                    else
                    {
                        var checkIfAnyTSignatureSavedInDbWithCurrentDate = await CheckIfAnyTSignatureSavedInDbWithCurrentDate();

                        if (checkIfAnyTSignatureSavedInDbWithCurrentDate.Count > 0)
                        {
                            IActionResult actionResult = customActionResult.Locked(
                                @$"You already clocked in. You must clock out before you clock in again. Your session already started at {checkIfAnyTSignatureSavedInDbWithCurrentDate[0].startDate}"
                            );
                            return StatusCode(StatusCodes.Status423Locked, actionResult);
                        }

                        else
                        {
                            if (body.wd_typeCode != "ZI")
                            {
                                IActionResult actionResult = customActionResult.BadRequest(
                                    @$"The workday 'code type' value provided by the client is not valid: {body.wd_typeCode}."
                                );
                                return StatusCode(StatusCodes.Status400BadRequest, actionResult);
                            }

                            else
                            {
                                var selectWorkdayById = await SelectWorkdayById();
                                   
                                if (selectWorkdayById.Count < 1)
                                {
                                    await InsertIntoWorkday(body);
                                }

                                var selectWorkdayByIdAgain = await SelectWorkdayById();

                                await ClockIn(body, selectWorkdayByIdAgain);

                                DateTime dateNow = new DateTime();
                                dateNow = DateTime.Now;

                                IActionResult actionResult = customActionResult.Ok(
                                    @$"You signed successfuly for flexible 'Time Signature' at {dateNow}. Don't forget to clock out."
                                );
                                return StatusCode(StatusCodes.Status200OK, actionResult);
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

        private async Task<List<TimeSignatureModel>> CheckIfAnyWorkdaySavedInDbWithCurrentDate()
        {
            DynamicParameters checkIfAnyWorkdaySavedInDbWithCurrentDate_params = new DynamicParameters();
            checkIfAnyWorkdaySavedInDbWithCurrentDate_params.Add("@userId", _claims.GetUserId());

            string checkIfAnyWorkdaySavedInDbWithCurrentDate_string = @$"
                SELECT TOP 1 [id], [endDate], [typeCode], [stateCode] FROM [TimeSignature] WHERE [endDate] IS NOT NULL AND [userId] = @userId ORDER BY [id] DESC
            ";

            var checkIfAnyWorkdaySavedInDbWithCurrentDate_results = await _db.SelectAsync<TimeSignatureModel, dynamic>(
               checkIfAnyWorkdaySavedInDbWithCurrentDate_string, checkIfAnyWorkdaySavedInDbWithCurrentDate_params
            );

            return checkIfAnyWorkdaySavedInDbWithCurrentDate_results;
        }

        private async Task<List<TimeSignatureModel>>CheckIfAnyTSignatureSavedInDbWithCurrentDate()
        {
            DynamicParameters checkIfClockInFlx_params = new DynamicParameters();
            checkIfClockInFlx_params.Add("@userId", _claims.GetUserId());

            string checkIfClockInFlx_string = @$"
                SELECT TOP 1 [id], [startDate] FROM [TimeSignature] WHERE [endDate] IS NULL AND [userId] = @userId ORDER BY [id] DESC
            ";

            var checkIfClockInFlx_results = await _db.SelectAsync<TimeSignatureModel, dynamic>(
                checkIfClockInFlx_string, checkIfClockInFlx_params
            );

            return checkIfClockInFlx_results;
        }

        private async Task<List<WorkdayModel>> SelectWorkdayById()
        {
            DynamicParameters selectWorkdayId_params = new DynamicParameters();
            selectWorkdayId_params.Add("@userId", _claims.GetUserId());

            string selectWorkdayId_string = $@"
                SELECT TOP 1 [id] 
                FROM [Workday] 
                WHERE [userId] = @userId 
                AND DATEDIFF(DAY, [date], CONVERT(DATE, GETDATE())) = 0 ORDER BY [id] DESC
            ";

            var selectWorkdayId = await _db.SelectAsync<WorkdayModel, dynamic>(selectWorkdayId_string, selectWorkdayId_params);
            return selectWorkdayId;
        }

        private async Task InsertIntoWorkday(TimeSignatureModel body)
        {
            DynamicParameters insertIntoWorkday_params = new DynamicParameters();
            insertIntoWorkday_params.Add("@wd_typeCode", body.wd_typeCode);
            insertIntoWorkday_params.Add("@userId", _claims.GetUserId());

            string insertIntoWorkday_string = $@"
                INSERT 
                INTO [Workday] (
                    [date], [startDate], [typeId], [typeCode], [userId]
                ) VALUES (
                    (SELECT GETDATE()),
                    (SELECT GETDATE()),
                    (SELECT [id] FROM [WorkdayType] WHERE [code] = @wd_typeCode),
                    (SELECT [code] FROM [WorkdayType] WHERE [code] = @wd_typeCode),
                    (SELECT [id] FROM [User] WHERE [id] = @userId)
                )
            ";

            await _db.InsertAsync(insertIntoWorkday_string, insertIntoWorkday_params);
        }

        private async Task ClockIn(TimeSignatureModel body, List<WorkdayModel> selectWorkdayById)
        {
            DynamicParameters clockInFlxSignatureWTypeZi_params = new DynamicParameters();
            clockInFlxSignatureWTypeZi_params.Add("@typeCode", body.typeCode);
            clockInFlxSignatureWTypeZi_params.Add("@wd_typeCode", body.wd_typeCode);
            clockInFlxSignatureWTypeZi_params.Add("@userId", _claims.GetUserId());
            clockInFlxSignatureWTypeZi_params.Add("@workdayId", selectWorkdayById[0].id);

            string clockInFlxSignatureWTypeZi_string = $@"
                INSERT 
                INTO [TimeSignature] (
                    [startDate], [typeCode], [stateCode], [wd_typeCode], [workdayId], [userId]
                ) VALUES (
                    (SELECT GETDATE()),
                    (SELECT [code] FROM [SignatureType] WHERE [code] = @typeCode),
                    (SELECT [code] FROM [SignatureState] WHERE [code] = 'IP'),
                    (SELECT [code] FROM [WorkdayType] WHERE [code] = @wd_typeCode),
                    (SELECT [id] FROM [Workday] WHERE [id] = @workdayId),
                    (SELECT [id] FROM [User] WHERE [id] = @userId)
                )
            ";

            await _db.InsertAsync(clockInFlxSignatureWTypeZi_string, clockInFlxSignatureWTypeZi_params);
        }
    }
}
