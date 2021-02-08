using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Server.Data;
using Server.Models;
using Server.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Server.Controllers
{
    [ApiController]
    public class ClockInFixTSignatureController : ControllerBase
    {
        private readonly ISqlDataAccess _db;
        private readonly IUserClaims _claims;

        private CustomActionResult customActionResult = new CustomActionResult();

        public ClockInFixTSignatureController(ISqlDataAccess db, IUserClaims claims)
        {
            _db = db;
            _claims = claims;
        }

        [Authorize]
        [HttpPost("new/api/user/clockIn/fixTSignature")]
        public async Task<IActionResult> ClockInAsOrdinary(TimeSignatureModel body)
        {
            try
            {

                //#region CHECK INPUTS SENT FROM CLIENT
                if (body == null)
                {
                    IActionResult actionResult = customActionResult.BadRequest(
                        "The client set the requested body to null before it was sent."
                    );
                    return StatusCode(StatusCodes.Status400BadRequest, actionResult);
                }

                else
                {
                    if (string.IsNullOrEmpty(body.typeCode) || string.IsNullOrWhiteSpace(body.typeCode))
                    {
                        IActionResult actionResult = customActionResult.FieldsRequired(
                            "The 'code type' field has been sent from the client null, empty, or whitespaced."
                        );
                        return StatusCode(StatusCodes.Status411LengthRequired, actionResult);
                    }

                    else if (body.typeCode != "FIX")
                    {
                        IActionResult actionResult = customActionResult.Conflict(
                            "This option is only for 'Fixed Time Signature'. For 'Flexible Time Signature', try the other option."
                        );
                        return StatusCode(StatusCodes.Status409Conflict, actionResult);
                    }

                    else
                    {
                        var selectTodaysUserTSignatureIdentifiersByUserId = await SelectTodaysUserTSignatureIdentifiersByUserId(body);

                        if (selectTodaysUserTSignatureIdentifiersByUserId.Count < 1)
                        {
                            if (string.IsNullOrEmpty(body.wd_typeCode) || string.IsNullOrWhiteSpace(body.wd_typeCode))
                            {
                                IActionResult actionResult = customActionResult.FieldsRequired(
                                   "The workday 'code type' field has been sent from the client null, empty, or whitespaced."
                               );
                                return StatusCode(StatusCodes.Status411LengthRequired, actionResult);
                            }

                            else if (body.fullDay == false && (body.startDate == null || body.endDate == null))
                            {
                                IActionResult actionResult = customActionResult.FieldsRequired(
                                    "The 'start date' or 'end date' field has been sent from the client null."
                                );
                                return StatusCode(StatusCodes.Status411LengthRequired, actionResult);
                            }

                            else if (body.fullDay == false && (body.startDate != null || body.endDate != null))
                            {
                                await InsertIntoWorkdayWithOutFullDayOption(body);

                                IActionResult actionResult = customActionResult.Ok(@$"You signed successfuly without full day option.");
                                return StatusCode(StatusCodes.Status200OK, actionResult);
                            }
                            

                            else if (body.fullDay == true && body.startDate == null)
                            {
                                IActionResult actionResult = customActionResult.FieldsRequired("The 'start date' field has been sent from the client null.");
                                return StatusCode(StatusCodes.Status411LengthRequired, actionResult);
                            }

                            else if (body.fullDay == true && body.startDate != null)
                            {
                                await InsertIntoWorkdayWithFullDayOption(body);

                                IActionResult actionResult = customActionResult.Ok(@$"You signed successfuly with full day option.");
                                return StatusCode(StatusCodes.Status200OK, actionResult);
                            }

                            else
                            {
                                IActionResult actionResult = customActionResult.NotFound(@$"No options found with given keys.");
                                return StatusCode(StatusCodes.Status404NotFound, actionResult);
                            }
                        }

                        else
                        {
                            var checkDbIfUserClockedInTodayFixTSignature = await CheckDbIfUserClockedInTodayWithFixTSignature(selectTodaysUserTSignatureIdentifiersByUserId);

                            if (
                                 checkDbIfUserClockedInTodayFixTSignature.Count > 0 && 
                                 selectTodaysUserTSignatureIdentifiersByUserId[0].stateCode == "TT" &&
                                 selectTodaysUserTSignatureIdentifiersByUserId[0].typeCode == "FIX"
                                )
                            {
                                if (string.IsNullOrEmpty(body.wd_typeCode) || string.IsNullOrWhiteSpace(body.wd_typeCode))
                                {
                                    IActionResult actionResult = customActionResult.FieldsRequired(
                                       "The workday 'code type' field has been sent from the client null, empty, or whitespaced."
                                    );
                                    return StatusCode(StatusCodes.Status411LengthRequired, actionResult);
                                }

                                else if (body.fullDay == false && (body.startDate == null || body.endDate == null))
                                {
                                    IActionResult actionResult = customActionResult.FieldsRequired(
                                        "The 'start date' or 'end date' field has been sent from the client null."
                                    );
                                    return StatusCode(StatusCodes.Status411LengthRequired, actionResult);
                                }

                                else if (body.fullDay == false && (body.startDate != null || body.endDate != null))
                                {
                                    await InsertIntoWorkdayWithOutFullDayOption(body);

                                    IActionResult actionResult = customActionResult.Ok("You signed successfuly without full day option.");
                                    return StatusCode(StatusCodes.Status200OK, actionResult);
                                }
                                

                                else if (body.fullDay == true && body.startDate == null)
                                {
                                        IActionResult actionResult = customActionResult.FieldsRequired(
                                            "The 'start date' field has been sent from the client null."
                                        );
                                        return StatusCode(StatusCodes.Status411LengthRequired, actionResult);
                                }

                                else if (body.fullDay == true && body.startDate != null)
                                {
                                    await InsertIntoWorkdayWithFullDayOption(body);
                                    IActionResult actionResult = customActionResult.Ok("You signed successfuly with full day option.");
                                    return StatusCode(StatusCodes.Status200OK, actionResult);
                                }
                                

                                else
                                {
                                    IActionResult actionResult = customActionResult.NotFound("No options found with given keys.");
                                    return StatusCode(StatusCodes.Status404NotFound, actionResult);
                                }
                            }

                            else
                            {
                                IActionResult actionResult = customActionResult.Locked(@$"
                                    You already clocked in for today with workday type code 'ZI'. 
                                    If you want to, you have the option to update your time signature. 
                                    Your session started at {selectTodaysUserTSignatureIdentifiersByUserId[0].startDate} 
                                    and ended at {selectTodaysUserTSignatureIdentifiersByUserId[0].endDate}
                                ");
                                return StatusCode(StatusCodes.Status423Locked, actionResult);
                            }
                        }
                    }
                }
            }

            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        //-------------------------------------------------------------------------------------------------------------------------------------//

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        [Authorize]
        [HttpPost("new/api/user/admin/clockIn/uid={id}/fixTSignature")]
        public async Task<IActionResult> ClockInUserAsAdmin(int id, TimeSignatureModel body)
        {
            try
            {

                //#region CHECK INPUTS SENT FROM CLIENT
                if (body == null)
                {
                    IActionResult actionResult = customActionResult.BadRequest("The client set the requested body to null before it was sent.");
                    return StatusCode(StatusCodes.Status400BadRequest, actionResult);
                }

                else
                {
                    if (string.IsNullOrEmpty(body.typeCode) || string.IsNullOrWhiteSpace(body.typeCode))
                    {
                        IActionResult actionResult = customActionResult.FieldsRequired(
                            "The 'code type' field has been sent from the client null, empty, or whitespaced."
                        );
                        return StatusCode(StatusCodes.Status411LengthRequired, actionResult);
                    }

                    else if (body.typeCode != "FIX")
                    {
                        IActionResult actionResult = customActionResult.Conflict(
                            "This option is only for 'Fixed Time Signature'. For 'Flexible Time Signature', try the other option."
                        );
                        return StatusCode(StatusCodes.Status409Conflict, actionResult);
                    }

                    else
                    {
                        var selectTodaysUserTSignatureIdentifiersByUserIdAsAdmin = await SelectTodaysUserTSignatureIdentifiersByUserIdAsAdmin(id, body);

                        if (selectTodaysUserTSignatureIdentifiersByUserIdAsAdmin.Count < 1)
                        {
                            if (string.IsNullOrEmpty(body.wd_typeCode) || string.IsNullOrWhiteSpace(body.wd_typeCode))
                            {
                                IActionResult actionResult = customActionResult.FieldsRequired(
                                   "The workday 'code type' field has been sent from the client null, empty, or whitespaced."
                               );
                                return StatusCode(StatusCodes.Status411LengthRequired, actionResult);
                            }

                            else if (body.fullDay == false && (body.startDate == null || body.endDate == null))
                            {
                                IActionResult actionResult = customActionResult.FieldsRequired(
                                    "The 'start date' or 'end date' field has been sent from the client null."
                                );
                                return StatusCode(StatusCodes.Status411LengthRequired, actionResult);
                            }

                            else if (body.fullDay == false && (body.startDate != null || body.endDate != null))
                            {
                                await InsertIntoWorkdayWithOutFullDayOptionAsAdmin(id, body);

                                IActionResult actionResult = customActionResult.Ok(@$"You signed successfuly without full day option.");
                                return StatusCode(StatusCodes.Status200OK, actionResult);
                            }


                            else if (body.fullDay == true && body.startDate == null)
                            {
                                IActionResult actionResult = customActionResult.FieldsRequired(
                                    "The 'start date' field has been sent from the client null."
                                );
                                return StatusCode(StatusCodes.Status411LengthRequired, actionResult);
                            }

                            else if(body.fullDay == true && body.startDate != null)
                            {
                                await InsertIntoWorkdayWithFullDayOptionAsAdmin(id, body);

                                IActionResult actionResult = customActionResult.Ok(@$"You signed successfuly with full day option.");
                                return StatusCode(StatusCodes.Status200OK, actionResult);
                            }

                            else
                            {
                                IActionResult actionResult = customActionResult.NotFound(@$"No options found with given values.");
                                return StatusCode(StatusCodes.Status404NotFound, actionResult);
                            }
                        }

                        else
                        {
                            var checkDbIfUserClockedInTodayFixTSignatureAsAdmin = await CheckDbIfUserClockedInTodayWithFixTSignatureAsAdmin(selectTodaysUserTSignatureIdentifiersByUserIdAsAdmin);

                            if (
                                 checkDbIfUserClockedInTodayFixTSignatureAsAdmin.Count > 0 &&
                                 selectTodaysUserTSignatureIdentifiersByUserIdAsAdmin[0].stateCode == "TT" &&
                                 selectTodaysUserTSignatureIdentifiersByUserIdAsAdmin[0].typeCode == "FIX"
                                )
                            {
                                if (string.IsNullOrEmpty(body.wd_typeCode) || string.IsNullOrWhiteSpace(body.wd_typeCode))
                                {
                                    IActionResult actionResult = customActionResult.FieldsRequired(
                                       "The workday 'code type' field has been sent from the client null, empty, or whitespaced."
                                    );
                                    return StatusCode(StatusCodes.Status411LengthRequired, actionResult);
                                }

                                else if (body.fullDay == false && (body.startDate == null || body.endDate == null))
                                {
                                   
                                    IActionResult actionResult = customActionResult.FieldsRequired(
                                        "The 'start date' or 'end date' field has been sent from the client null."
                                    );
                                    return StatusCode(StatusCodes.Status411LengthRequired, actionResult);
                                }

                                else if (body.fullDay == false && (body.startDate != null || body.endDate != null))
                                {
                                        await InsertIntoWorkdayWithOutFullDayOptionAsAdmin(id, body);

                                    IActionResult actionResult = customActionResult.Ok("You signed successfuly without full day option.");
                                    return StatusCode(StatusCodes.Status200OK, actionResult);
                                }

                                else if (body.fullDay == true && body.startDate == null)
                                {
                                    IActionResult actionResult = customActionResult.FieldsRequired(
                                        "The 'start date' field has been sent from the client null."
                                    );
                                    return StatusCode(StatusCodes.Status411LengthRequired, actionResult);
                                }

                                else if (body.fullDay == true && body.startDate != null)
                                {
                                    await InsertIntoWorkdayWithFullDayOptionAsAdmin(id, body);
                                    IActionResult actionResult = customActionResult.Ok("You signed successfuly with full day option.");
                                    return StatusCode(StatusCodes.Status200OK, actionResult);
                                }

                                else
                                {
                                    IActionResult actionResult = customActionResult.NotFound("No options found with given keys.");
                                    return StatusCode(StatusCodes.Status404NotFound, actionResult);
                                }
                            }

                            else
                            {
                                IActionResult actionResult = customActionResult.Locked(@$"
                                    User already clocked in for today. If you want to, you have the option to update this time signature. 
                                    User's session started at {selectTodaysUserTSignatureIdentifiersByUserIdAsAdmin[0].startDate} 
                                    and ended at {selectTodaysUserTSignatureIdentifiersByUserIdAsAdmin[0].endDate}
                                ");
                                return StatusCode(StatusCodes.Status423Locked, actionResult);
                            }
                        }
                    }
                }
            }

            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        //-------------------------------------------------------------------------------------------------------------------------------------//

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        [Route("new/api/[controller]")]
        private async Task<List<TimeSignatureModel>> SelectTodaysUserTSignatureIdentifiersByUserId(TimeSignatureModel body)
        {
            DynamicParameters selectTodaysUserTSignatureIdentifiersByUserId_params = new DynamicParameters();
            selectTodaysUserTSignatureIdentifiersByUserId_params.Add("@startDate", body.startDate);
            selectTodaysUserTSignatureIdentifiersByUserId_params.Add("@userId", _claims.GetUserId());

            string selectTodaysUserTSignatureIdentifiersByUserId_string = @$"
                SELECT TOP 1 [startDate], [endDate], [typeCode], [stateCode], [workdayId] 
                FROM [TimeSignature]
                WHERE [userId] = @userId
                AND (SELECT DATEDIFF(DAY, [startDate], @startDate)) = 0
            ";

            var selectTodaysUserTSignatureIdentifiersByUserId_results = await _db.SelectAsync<TimeSignatureModel, dynamic>(
                selectTodaysUserTSignatureIdentifiersByUserId_string, selectTodaysUserTSignatureIdentifiersByUserId_params
            );

            return selectTodaysUserTSignatureIdentifiersByUserId_results;
        }

        private async Task<List<TimeSignatureModel>> CheckDbIfUserClockedInTodayWithFixTSignature(List<TimeSignatureModel> TSignatureIdentifiersFromDbResults)
        {
            DynamicParameters checkIfUserClockedInTodayFixTSignature_params = new DynamicParameters();
            checkIfUserClockedInTodayFixTSignature_params.Add("@workdayId", TSignatureIdentifiersFromDbResults[0].workdayId);
            checkIfUserClockedInTodayFixTSignature_params.Add("@startDate", TSignatureIdentifiersFromDbResults[0].startDate);

            string checkIfUserClockedInTodayFixTSignature_string = @$"
                SELECT [startDate]
                FROM [TimeSignature]
                WHERE [workdayId] = @workdayId
                AND (SELECT DATEDIFF(DAY, [startDate], @startDate)) < 0
            ";

            var checkIfUserClockedInTodayFixTSignature_results = await _db.SelectAsync<TimeSignatureModel, dynamic>(
                checkIfUserClockedInTodayFixTSignature_string, checkIfUserClockedInTodayFixTSignature_params
             );

            return checkIfUserClockedInTodayFixTSignature_results;
        }

        [Route("new/api/[controller]")]
        private async Task InsertIntoWorkdayWithOutFullDayOption(TimeSignatureModel body)
        {
            DynamicParameters insertIntoWorkday_params = new DynamicParameters();
            insertIntoWorkday_params.Add("@startDate", body.startDate);
            insertIntoWorkday_params.Add("@endDate", body.endDate);
            insertIntoWorkday_params.Add("@wd_typeCode", body.wd_typeCode);
            insertIntoWorkday_params.Add("@userId", _claims.GetUserId());

            string insertIntoWorkday_string = $@"
                INSERT 
                INTO [Workday] (
                    [date], [startDate], [endDate], [aom], [typeId], [typeCode], [userId]
                ) VALUES (
                    @startDate,
                    @startDate,
                    @endDate,
                    (SELECT DATEDIFF(MINUTE, @startDate, @endDate)),
                    (SELECT [id] FROM [WorkdayType] WHERE [code] = @wd_typeCode),
                    (SELECT [code] FROM [WorkdayType] WHERE [code] = @wd_typeCode),
                    (SELECT [id] FROM [User] WHERE [id] = @userId)
                )
            ";

            await _db.InsertAsync(insertIntoWorkday_string, insertIntoWorkday_params);

            DynamicParameters selectWorkdayId_params = new DynamicParameters();
            selectWorkdayId_params.Add("@startDate", body.startDate);
            selectWorkdayId_params.Add("@userId", _claims.GetUserId());

            string selectWorkdayId_string = $@"
                SELECT TOP 1 [id] FROM [Workday] WHERE [userId] = @userId AND DATEDIFF(MINUTE, [startDate], @startDate) < 1 ORDER BY [id] DESC
            ";

            var selectWorkdayId = await _db.SelectAsync<TimeSignatureModel, dynamic>(
                selectWorkdayId_string, selectWorkdayId_params
            );

            DynamicParameters wdTypeZi_params = new DynamicParameters();
            wdTypeZi_params.Add("@startDate", body.startDate);
            wdTypeZi_params.Add("@endDate", body.endDate);
            wdTypeZi_params.Add("@typeCode", body.typeCode);
            wdTypeZi_params.Add("@wd_typeCode", body.wd_typeCode);
            wdTypeZi_params.Add("@userId", _claims.GetUserId());
            wdTypeZi_params.Add("@workdayId", selectWorkdayId[0].id);

            string clockInFixSignatureWTypeZiString = $@"
                INSERT 
                INTO [TimeSignature] (
                    [startDate], [endDate], [typeCode], [stateCode], 
                    [wd_typeCode], [workdayId], [userId], [aom]
                ) VALUES (
                    @startDate,
                    @endDate, 
                    (SELECT [code] FROM [SignatureType] WHERE [code] = @typeCode),
                    (SELECT [code] FROM [SignatureState] WHERE [code] = 'TT'),
                    (SELECT [code] FROM [WorkdayType] WHERE [code] = @wd_typeCode),
                    (SELECT [id] FROM [Workday] WHERE[id] = @workdayId),
                    (SELECT [id] FROM [User] WHERE [id] = @userId),
                    (SELECT DATEDIFF(MINUTE, @startDate, @endDate))
                )
            ";

            await _db.InsertAsync(clockInFixSignatureWTypeZiString, wdTypeZi_params);
        }

        [Route("new/api/[controller]")]
        private async Task InsertIntoWorkdayWithFullDayOption(TimeSignatureModel body)
        {
            DynamicParameters insertIntoWorkday_params = new DynamicParameters();
            insertIntoWorkday_params.Add("@startDate", body.startDate);
            insertIntoWorkday_params.Add("@wd_typeCode", body.wd_typeCode);
            insertIntoWorkday_params.Add("@userId", _claims.GetUserId());

            string insertIntoWorkday_string = $@"
                INSERT 
                INTO [Workday] (
                    [date], [startDate], [endDate], [aom], [typeId], [typeCode], [userId]
                ) VALUES (
                    @startDate,
                    @startDate,
                    (SELECT DATEADD(MINUTE, 480, @startDate)),
                    '480',
                    (SELECT [id] FROM [WorkdayType] WHERE [code] = @wd_typeCode),
                    (SELECT [code] FROM [WorkdayType] WHERE [code] = @wd_typeCode),
                    (SELECT [id] FROM [User] WHERE [id] = @userId)
                )
            ";

            await _db.InsertAsync(insertIntoWorkday_string, insertIntoWorkday_params);

            DynamicParameters selectWorkdayId_params = new DynamicParameters();
            selectWorkdayId_params.Add("@startDate", body.startDate);
            selectWorkdayId_params.Add("@userId", _claims.GetUserId());

            string selectWorkdayId_string = $@"
                SELECT TOP 1 [id] FROM [Workday] WHERE [userId] = @userId AND DATEDIFF(MINUTE, [startDate], @startDate) < 1 ORDER BY [id] DESC
            ";

            var selectWorkdayId = await _db.SelectAsync<TimeSignatureModel, dynamic>(
                selectWorkdayId_string, selectWorkdayId_params
            );

            DynamicParameters clockInFixSignatureWTypeZiFullDay_params = new DynamicParameters();
            clockInFixSignatureWTypeZiFullDay_params.Add("@startDate", body.startDate);
            clockInFixSignatureWTypeZiFullDay_params.Add("@typeCode", body.typeCode);
            clockInFixSignatureWTypeZiFullDay_params.Add("@wd_typeCode", body.wd_typeCode);
            clockInFixSignatureWTypeZiFullDay_params.Add("@userId", _claims.GetUserId());
            clockInFixSignatureWTypeZiFullDay_params.Add("@workdayId", selectWorkdayId[0].id);

            string clockInFixSignatureWTypeZi_string = $@"
                INSERT 
                INTO [TimeSignature] (
                    [startDate], [endDate], [typeCode], [stateCode], [wd_typeCode],
                    [userId], [workdayId], [aom]
                ) VALUES (
                    @startDate,
                    (SELECT DATEADD(MINUTE, 480, @startDate)), 
                    (SELECT [code] FROM [SignatureType] WHERE [code] = @typeCode),
                    (SELECT [code] FROM [SignatureState] WHERE [code] = 'TT'),
                    (SELECT [code] FROM [WorkdayType] WHERE [code] = @wd_typeCode),
                    (SELECT [id] FROM [User] WHERE [id] = @userId),
                    (SELECT [id] FROM [Workday] WHERE [id] = @workdayId),
                    '480'
                )
            ";

            await _db.InsertAsync(clockInFixSignatureWTypeZi_string, clockInFixSignatureWTypeZiFullDay_params);
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        //-------------------------------------------------------------------------------------------------------------------------------------//

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        [Route("new/api/[controller]")]
        private async Task<List<TimeSignatureModel>> SelectTodaysUserTSignatureIdentifiersByUserIdAsAdmin(int id, TimeSignatureModel body)
        {
            DynamicParameters selectTodaysUserTSignatureIdentifiersByUserId_params = new DynamicParameters();
            selectTodaysUserTSignatureIdentifiersByUserId_params.Add("@startDate", body.startDate);
            selectTodaysUserTSignatureIdentifiersByUserId_params.Add("@userId", id);

            string selectTodaysUserTSignatureIdentifiersByUserId_string = @$"
                SELECT TOP 1 [startDate], [endDate], [typeCode], [stateCode], [workdayId] 
                FROM [TimeSignature]
                WHERE [userId] = @userId
                AND (SELECT DATEDIFF(DAY, [startDate], @startDate)) = 0
            ";

            var selectTodaysUserTSignatureIdentifiersByUserId_results = await _db.SelectAsync<TimeSignatureModel, dynamic>(
                selectTodaysUserTSignatureIdentifiersByUserId_string, selectTodaysUserTSignatureIdentifiersByUserId_params
            );

            return selectTodaysUserTSignatureIdentifiersByUserId_results;
        }

        private async Task<List<TimeSignatureModel>> CheckDbIfUserClockedInTodayWithFixTSignatureAsAdmin(List<TimeSignatureModel> TSignatureIdentifiersFromDbResults)
        {
            DynamicParameters checkIfUserClockedInTodayFixTSignature_params = new DynamicParameters();
            checkIfUserClockedInTodayFixTSignature_params.Add("@workdayId", TSignatureIdentifiersFromDbResults[0].workdayId);
            checkIfUserClockedInTodayFixTSignature_params.Add("@startDate", TSignatureIdentifiersFromDbResults[0].startDate);

            string checkIfUserClockedInTodayFixTSignature_string = @$"
                SELECT [startDate]
                FROM [TimeSignature]
                WHERE [workdayId] = @workdayId
                AND (SELECT DATEDIFF(DAY, [startDate], @startDate)) < 0
            ";

            var checkIfUserClockedInTodayFixTSignature_results = await _db.SelectAsync<TimeSignatureModel, dynamic>(
                checkIfUserClockedInTodayFixTSignature_string, checkIfUserClockedInTodayFixTSignature_params
             );

            return checkIfUserClockedInTodayFixTSignature_results;
        }

        [Route("new/api/[controller]")]
        private async Task InsertIntoWorkdayWithOutFullDayOptionAsAdmin(int id, TimeSignatureModel body)
        {
            DynamicParameters insertIntoWorkday_params = new DynamicParameters();
            insertIntoWorkday_params.Add("@startDate", body.startDate);
            insertIntoWorkday_params.Add("@endDate", body.endDate);
            insertIntoWorkday_params.Add("@wd_typeCode", body.wd_typeCode);
            insertIntoWorkday_params.Add("@userId", id);

            string insertIntoWorkday_string = $@"
                INSERT 
                INTO [Workday] (
                    [date], [startDate], [endDate], [aom], [typeId], [typeCode], [userId]
                ) VALUES (
                    @startDate,
                    @startDate,
                    @endDate,
                    (SELECT DATEDIFF(MINUTE, @startDate, @endDate)),
                    (SELECT [id] FROM [WorkdayType] WHERE [code] = @wd_typeCode),
                    (SELECT [code] FROM [WorkdayType] WHERE [code] = @wd_typeCode),
                    (SELECT [id] FROM [User] WHERE [id] = @userId)
                )
            ";

            await _db.InsertAsync(insertIntoWorkday_string, insertIntoWorkday_params);

            DynamicParameters selectWorkdayId_params = new DynamicParameters();
            selectWorkdayId_params.Add("@startDate", body.startDate);
            selectWorkdayId_params.Add("@userId", id);

            string selectWorkdayId_string = $@"
                SELECT TOP 1 [id] FROM [Workday] WHERE [userId] = @userId AND DATEDIFF(MINUTE, [startDate], @startDate) < 1 ORDER BY [id] DESC
            ";

            var selectWorkdayId = await _db.SelectAsync<TimeSignatureModel, dynamic>(
                selectWorkdayId_string, selectWorkdayId_params
            );

            DynamicParameters wdTypeZi_params = new DynamicParameters();
            wdTypeZi_params.Add("@startDate", body.startDate);
            wdTypeZi_params.Add("@endDate", body.endDate);
            wdTypeZi_params.Add("@typeCode", body.typeCode);
            wdTypeZi_params.Add("@wd_typeCode", body.wd_typeCode);
            wdTypeZi_params.Add("@userId", id);
            wdTypeZi_params.Add("@workdayId", selectWorkdayId[0].id);

            string clockInFixSignatureWTypeZiString = $@"
                INSERT 
                INTO [TimeSignature] (
                    [startDate], [endDate], [typeCode], [stateCode], 
                    [wd_typeCode], [workdayId], [userId], [aom]
                ) VALUES (
                    @startDate,
                    @endDate, 
                    (SELECT [code] FROM [SignatureType] WHERE [code] = @typeCode),
                    (SELECT [code] FROM [SignatureState] WHERE [code] = 'TT'),
                    (SELECT [code] FROM [WorkdayType] WHERE [code] = @wd_typeCode),
                    (SELECT [id] FROM [Workday] WHERE[id] = @workdayId),
                    (SELECT [id] FROM [User] WHERE [id] = @userId),
                    (SELECT DATEDIFF(MINUTE, @startDate, @endDate))
                )
            ";

            await _db.InsertAsync(clockInFixSignatureWTypeZiString, wdTypeZi_params);
        }

        [Route("new/api/[controller]")]
        private async Task InsertIntoWorkdayWithFullDayOptionAsAdmin(int id, TimeSignatureModel body)
        {
            DynamicParameters insertIntoWorkday_params = new DynamicParameters();
            insertIntoWorkday_params.Add("@startDate", body.startDate);
            insertIntoWorkday_params.Add("@wd_typeCode", body.wd_typeCode);
            insertIntoWorkday_params.Add("@userId", id);

            string insertIntoWorkday_string = $@"
                INSERT 
                INTO [Workday] (
                    [date], [startDate], [endDate], [aom], [typeId], [typeCode], [userId]
                ) VALUES (
                    @startDate,
                    @startDate,
                    (SELECT DATEADD(MINUTE, 480, @startDate)),
                    '480',
                    (SELECT [id] FROM [WorkdayType] WHERE [code] = @wd_typeCode),
                    (SELECT [code] FROM [WorkdayType] WHERE [code] = @wd_typeCode),
                    (SELECT [id] FROM [User] WHERE [id] = @userId)
                )
            ";

            await _db.InsertAsync(insertIntoWorkday_string, insertIntoWorkday_params);

            DynamicParameters selectWorkdayId_params = new DynamicParameters();
            selectWorkdayId_params.Add("@startDate", body.startDate);
            selectWorkdayId_params.Add("@userId", id);

            string selectWorkdayId_string = $@"
                SELECT TOP 1 [id] FROM [Workday] WHERE [userId] = @userId AND DATEDIFF(MINUTE, [startDate], @startDate) < 1 ORDER BY [id] DESC
            ";

            var selectWorkdayId = await _db.SelectAsync<TimeSignatureModel, dynamic>(
                selectWorkdayId_string, selectWorkdayId_params
            );

            DynamicParameters clockInFixSignatureWTypeZiFullDay_params = new DynamicParameters();
            clockInFixSignatureWTypeZiFullDay_params.Add("@startDate", body.startDate);
            clockInFixSignatureWTypeZiFullDay_params.Add("@typeCode", body.typeCode);
            clockInFixSignatureWTypeZiFullDay_params.Add("@wd_typeCode", body.wd_typeCode);
            clockInFixSignatureWTypeZiFullDay_params.Add("@userId", id);
            clockInFixSignatureWTypeZiFullDay_params.Add("@workdayId", selectWorkdayId[0].id);

            string clockInFixSignatureWTypeZi_string = $@"
                INSERT 
                INTO [TimeSignature] (
                    [startDate], [endDate], [typeCode], [stateCode], [wd_typeCode],
                    [userId], [workdayId], [aom]
                ) VALUES (
                    @startDate,
                    (SELECT DATEADD(MINUTE, 480, @startDate)), 
                    (SELECT [code] FROM [SignatureType] WHERE [code] = @typeCode),
                    (SELECT [code] FROM [SignatureState] WHERE [code] = 'TT'),
                    (SELECT [code] FROM [WorkdayType] WHERE [code] = @wd_typeCode),
                    (SELECT [id] FROM [User] WHERE [id] = @userId),
                    (SELECT [id] FROM [Workday] WHERE [id] = @workdayId),
                    '480'
                )
            ";

            await _db.InsertAsync(clockInFixSignatureWTypeZi_string, clockInFixSignatureWTypeZiFullDay_params);
        }
    }
}