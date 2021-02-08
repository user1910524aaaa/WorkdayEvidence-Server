using System;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Server.Controllers;

using Server.Data;
using Server.Models;
using Server.Services;

namespace Server.Controllers
{
    [ApiController]
    public class UpdateWorkdayController : ControllerBase
    {
        private readonly ISqlDataAccess _db;
        private readonly IUserClaims _claims;

        private CustomActionResult customActionResult = new CustomActionResult();

        public UpdateWorkdayController(ISqlDataAccess db, IUserClaims claims)
        {
            _db = db;
            _claims = claims;
        }

        [Authorize]
        [HttpPut("new/api/user/update/wid={id}")]
        public async Task<IActionResult> UpdateSelfsWorkdayAndTSignatureByWorkdayId(int id, TimeSignatureModel body)
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
                    if (body.startDate == null || body.endDate == null)
                    {
                        IActionResult actionResult = customActionResult.FieldsRequired(
                            "The 'start date' or 'end date' field has been sent from the client null, empty, or whitespaced."
                        );
                        return StatusCode(StatusCodes.Status411LengthRequired, actionResult);
                    }

                    else
                    {
                        DynamicParameters selectTSignaturesIdentifiersByWorkdayIdAndUserId_params = new DynamicParameters();
                        selectTSignaturesIdentifiersByWorkdayIdAndUserId_params.Add("@workdayId", id);
                        selectTSignaturesIdentifiersByWorkdayIdAndUserId_params.Add("@userId", _claims.GetUserId());


                        string selectTSignaturesIdentifiersByWorkdayIdAndUserId_string = $@"
                            SELECT [typeCode], [updateBy] FROM [TimeSignature] WHERE [workdayId] = @workdayId AND [userId] = @userId
                        ";

                        var selectTSignaturesIdentifiersByWorkdayIdAndUserId = await _db.SelectAsync<TimeSignatureModel, dynamic>(
                            selectTSignaturesIdentifiersByWorkdayIdAndUserId_string, selectTSignaturesIdentifiersByWorkdayIdAndUserId_params
                        );

                        if (
                            selectTSignaturesIdentifiersByWorkdayIdAndUserId.Count > 0 &&
                            selectTSignaturesIdentifiersByWorkdayIdAndUserId[0].typeCode == "FIX" &&
                            (
                                selectTSignaturesIdentifiersByWorkdayIdAndUserId[0].updateBy == null || 
                                selectTSignaturesIdentifiersByWorkdayIdAndUserId[0].updateBy == "self"
                            )
                           )
                        {
                            DynamicParameters updateTSignature_params = new DynamicParameters();
                            updateTSignature_params.Add("@userId", _claims.GetUserId());
                            updateTSignature_params.Add("@userName", _claims.GetUserName());
                            updateTSignature_params.Add("@id", id);
                            updateTSignature_params.Add("@startDate", body.startDate);
                            updateTSignature_params.Add("@endDate", body.endDate);
                            updateTSignature_params.Add("@wd_typeCode", body.wd_typeCode);

                            string updateTSignature_string = $@"
                                UPDATE [TimeSignature] 
                                SET
                                    [startDate] = @startDate,
                                    [endDate] = @endDate,
                                    [aom] = (SELECT DATEDIFF(MINUTE, @startDate, @endDate)),
                                    [wd_typeCode] = @wd_typeCode,
                                    [lastUpdate] = GETDATE(),
                                    [updateBy] = 'self'
                                WHERE 
                                    [userId] = @userId 
                                AND [workdayId] = @id
                            ";

                            await _db.UpdateAsync(updateTSignature_string, updateTSignature_params);

                            string updateWorkday_string = $@"
                                UPDATE [Workday] 
                                SET
                                    [startDate] = @startDate,
                                    [endDate] = @endDate,
                                    [aom] = (SELECT DATEDIFF(MINUTE, @startDate, @endDate)),
                                    [typeCode] = @wd_typeCode
                                WHERE 
                                    [userId] = @userId 
                                AND [id] = @id
                            ";

                            await _db.UpdateAsync(updateWorkday_string, updateTSignature_params);

                            IActionResult actionResult = customActionResult.Ok(@$"Your time signature was successfully updated.");
                            return StatusCode(StatusCodes.Status200OK, actionResult);
                        }

                        else
                        if (
                            selectTSignaturesIdentifiersByWorkdayIdAndUserId.Count > 0 &&
                            selectTSignaturesIdentifiersByWorkdayIdAndUserId[0].typeCode == "FIX" &&
                            (
                                selectTSignaturesIdentifiersByWorkdayIdAndUserId[0].updateBy != null || 
                                selectTSignaturesIdentifiersByWorkdayIdAndUserId[0].updateBy != "self"
                            )
                           )
                        {
                            IActionResult actionResult = customActionResult.Unauthorized(
                                "You are not authorzied to make any updates on workdays having time signatures that were updated by an admin user."
                            );
                            return StatusCode(StatusCodes.Status401Unauthorized, actionResult);
                        }

                        else 
                        if (
                            selectTSignaturesIdentifiersByWorkdayIdAndUserId.Count > 0 &&
                            selectTSignaturesIdentifiersByWorkdayIdAndUserId[0].typeCode == "FLX" &&
                            _claims.GetUserRole() == "admin"
                           )
                        {
                                DynamicParameters deleteFlxTSignaturesByWorkdayId_params = new DynamicParameters();
                                deleteFlxTSignaturesByWorkdayId_params.Add("@id", id);

                                string deleteFlxTSignaturesByWorkdayId = $@"
                                    DELETE FROM [TimeSignature] WHERE [WorkdayId] = @id
                                ";
                                await _db.DeleteAsync(deleteFlxTSignaturesByWorkdayId, deleteFlxTSignaturesByWorkdayId_params);

                                DynamicParameters insertIntoTSignature_params = new DynamicParameters();
                                insertIntoTSignature_params.Add("@userId", _claims.GetUserId());
                                insertIntoTSignature_params.Add("@workdayId", id);
                                insertIntoTSignature_params.Add("@wd_typeCode", body.wd_typeCode);
                                insertIntoTSignature_params.Add("@startDate", body.startDate);
                                insertIntoTSignature_params.Add("@endDate", body.endDate);

                                string insertIntoTSignature_string = $@"
                                    INSERT 
                                    INTO [TimeSignature] (
                                        [startDate], [endDate], [typeCode], [stateCode], 
                                        [wd_typeCode], [workdayId], [userId], [aom], [lastUpdate], [updateBy]
                                    ) VALUES (
                                        @startDate,
                                        @endDate, 
                                        (SELECT [code] FROM [SignatureType] WHERE [code] = 'FIX'),
                                        (SELECT [code] FROM [SignatureState] WHERE [code] = 'TT'),
                                        (SELECT [code] FROM [WorkdayType] WHERE [code] = @wd_typeCode),
                                        (SELECT [id] FROM [Workday] WHERE [id] = @workdayId),
                                        (SELECT [id] FROM [User] WHERE [id] = @userId),
                                        (SELECT DATEDIFF(MINUTE, @startDate, @endDate)),
                                        (SELECT GETDATE()),
                                        'self'
                                    )
                                ";

                                await _db.InsertAsync(insertIntoTSignature_string, insertIntoTSignature_params);

                                DynamicParameters updateWorkdayById_params = new DynamicParameters();
                                updateWorkdayById_params.Add("@userId", _claims.GetUserId());
                                updateWorkdayById_params.Add("@workdayId", id);
                                updateWorkdayById_params.Add("@startDate", body.startDate);
                                updateWorkdayById_params.Add("@endDate", body.endDate);

                                string updateWorkdayById_string = $@"
                                        UPDATE [Workday] 
                                        SET
                                            [date] = (SELECT [startDate] FROM [TimeSignature] WHERE [workdayId] = @workdayId),
                                            [startDate] = (SELECT [startDate] FROM [TimeSignature] WHERE [workdayId] = @workdayId),
                                            [endDate] = (SELECT [endDate] FROM [TimeSignature] WHERE [workdayId] = @workdayId),
                                            [aom] = (SELECT DATEDIFF(MINUTE, @startDate, @endDate))
                                        WHERE [userId] = @userId 
                                        AND [id] = @workdayId
                                    ";
                                await _db.UpdateAsync(updateWorkdayById_string, updateWorkdayById_params);

                                IActionResult actionResult = customActionResult.Ok(@$"Your time signature was successfully updated.");
                                return StatusCode(StatusCodes.Status200OK, actionResult);
                        }

                        else
                        if(
                            selectTSignaturesIdentifiersByWorkdayIdAndUserId.Count > 0 &&
                            selectTSignaturesIdentifiersByWorkdayIdAndUserId[0].typeCode == "FLX" &&
                            _claims.GetUserRole() != "admin"
                           )
                        {
                            IActionResult actionResult = customActionResult.Unauthorized(
                                "You are not authorized to update your workdays with flexible time signature, if by mystake you made a databes record using flexible time signature, please contact your manager."
                            );
                            return StatusCode(StatusCodes.Status401Unauthorized, actionResult);
                        }
                        else
                        {
                            IActionResult actionResult = customActionResult.NotFound(
                                @$"No time signatures found searching by workday id and user id."
                            );
                            return StatusCode(StatusCodes.Status404NotFound, actionResult);
                        }
                    }
                }
            }

            catch(Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        [Authorize]
        [HttpPut("new/api/user/admin/update/wid={id}")]
        public async Task<IActionResult> UpdateAnotherUsersWorkdayAndTSignatureByWorkdayIdAsAdmin(int id, TimeSignatureModel body)
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
                    if (body.startDate == null || body.endDate == null)
                    {
                        IActionResult actionResult = customActionResult.FieldsRequired(
                            "The 'start date' or 'end date' field has been sent from the client null, empty, or whitespaced."
                        );
                        return StatusCode(StatusCodes.Status411LengthRequired, actionResult);
                    }

                    else
                    {
                        if(_claims.GetUserRole() != "admin")
                        {
                            IActionResult actionResult = customActionResult.Unauthorized(
                                "You are not authorized to use this api endpoint to make any apdates."
                            );
                            return StatusCode(StatusCodes.Status401Unauthorized, actionResult);
                        }

                        else
                        {
                            DynamicParameters selectTSignaturesIdentifiersByWorkdayIdAndUserId_params = new DynamicParameters();
                            selectTSignaturesIdentifiersByWorkdayIdAndUserId_params.Add("@workdayId", id);

                            string selectTSignaturesIdentifiersByWorkdayIdAndUserId_string = $@"
                                SELECT [typeCode], [userId] FROM [TimeSignature] WHERE [workdayId] = @workdayId
                            ";

                            var selectTSignaturesIdentifiersByWorkdayIdAndUserId = await _db.SelectAsync<TimeSignatureModel, dynamic>(
                                selectTSignaturesIdentifiersByWorkdayIdAndUserId_string, selectTSignaturesIdentifiersByWorkdayIdAndUserId_params
                            );

                            if (
                                selectTSignaturesIdentifiersByWorkdayIdAndUserId.Count > 0 &&
                                selectTSignaturesIdentifiersByWorkdayIdAndUserId[0].typeCode == "FIX"
                               )
                            {
                                DynamicParameters updateTSignature_params = new DynamicParameters();
                                updateTSignature_params.Add("@userId", selectTSignaturesIdentifiersByWorkdayIdAndUserId[0].userId);
                                updateTSignature_params.Add("@userName", _claims.GetUserName());
                                updateTSignature_params.Add("@workdayId", id);
                                updateTSignature_params.Add("@startDate", body.startDate);
                                updateTSignature_params.Add("@endDate", body.endDate);
                                updateTSignature_params.Add("@wd_typeCode", body.wd_typeCode);

                                string updateTSignature_string = $@"
                                    UPDATE [TimeSignature] 
                                    SET
                                        [startDate] = @startDate,
                                        [endDate] = @endDate,
                                        [aom] = (SELECT DATEDIFF(MINUTE, @startDate, @endDate)),
                                        [wd_typeCode] = @wd_typeCode,
                                        [lastUpdate] = GETDATE(),
                                        [updateBy] = @userName
                                    WHERE 
                                        [userId] = @userId 
                                    AND [workdayId] = @workdayId
                                ";

                                await _db.UpdateAsync(updateTSignature_string, updateTSignature_params);

                                string updateWorkday_string = $@"
                                    UPDATE [Workday] 
                                    SET
                                        [startDate] = @startDate,
                                        [endDate] = @endDate,
                                        [aom] = (SELECT DATEDIFF(MINUTE, @startDate, @endDate)),
                                        [typeCode] = @wd_typeCode
                                    WHERE 
                                        [userId] = @userId 
                                    AND [id] = @workdayId
                                ";

                                await _db.UpdateAsync(updateWorkday_string, updateTSignature_params);

                                IActionResult actionResult = customActionResult.Ok(@$"Your time signature was successfully updated.");
                                return StatusCode(StatusCodes.Status200OK, actionResult);
                            }

                            else
                            if (
                                selectTSignaturesIdentifiersByWorkdayIdAndUserId.Count > 0 &&
                                selectTSignaturesIdentifiersByWorkdayIdAndUserId[0].typeCode == "FLX"
                               )
                            {
                                DynamicParameters deleteFlxTSignaturesByWorkdayId_params = new DynamicParameters();
                                deleteFlxTSignaturesByWorkdayId_params.Add("@id", id);
                                deleteFlxTSignaturesByWorkdayId_params.Add("@userId", selectTSignaturesIdentifiersByWorkdayIdAndUserId[0].userId);

                                string deleteFlxTSignaturesByWorkdayId = $@"
                                    DELETE FROM [TimeSignature] WHERE [WorkdayId] = @id AND [userId] = @userId
                                ";
                                await _db.DeleteAsync(deleteFlxTSignaturesByWorkdayId, deleteFlxTSignaturesByWorkdayId_params);

                                DynamicParameters insertIntoTSignature_params = new DynamicParameters();
                                insertIntoTSignature_params.Add("@userId", selectTSignaturesIdentifiersByWorkdayIdAndUserId[0].userId);
                                insertIntoTSignature_params.Add("@adminUserName", _claims.GetUserName());
                                insertIntoTSignature_params.Add("@workdayId", id);
                                insertIntoTSignature_params.Add("@wd_typeCode", body.wd_typeCode);
                                insertIntoTSignature_params.Add("@startDate", body.startDate);
                                insertIntoTSignature_params.Add("@endDate", body.endDate);

                                string insertIntoTSignature_string = $@"
                                    INSERT 
                                    INTO [TimeSignature] (
                                        [startDate], [endDate], [typeCode], [stateCode], 
                                        [wd_typeCode], [workdayId], [userId], [aom], [lastUpdate], [updateBy]
                                    ) VALUES (
                                        @startDate,
                                        @endDate, 
                                        (SELECT [code] FROM [SignatureType] WHERE [code] = 'FIX'),
                                        (SELECT [code] FROM [SignatureState] WHERE [code] = 'TT'),
                                        (SELECT [code] FROM [WorkdayType] WHERE [code] = @wd_typeCode),
                                        (SELECT [id] FROM [Workday] WHERE [id] = @workdayId),
                                        (SELECT [id] FROM [User] WHERE [id] = @userId),
                                        (SELECT DATEDIFF(MINUTE, @startDate, @endDate)),
                                        (SELECT GETDATE()),
                                        @adminUserName
                                    )
                                ";

                                await _db.InsertAsync(insertIntoTSignature_string, insertIntoTSignature_params);

                                DynamicParameters updateWorkdayById_params = new DynamicParameters();
                                updateWorkdayById_params.Add("@userId", selectTSignaturesIdentifiersByWorkdayIdAndUserId[0].userId);
                                updateWorkdayById_params.Add("@workdayId", id);
                                updateWorkdayById_params.Add("@startDate", body.startDate);
                                updateWorkdayById_params.Add("@endDate", body.endDate);

                                string updateWorkdayById_string = $@"
                                        UPDATE [Workday] 
                                        SET
                                            [date] = (SELECT [startDate] FROM [TimeSignature] WHERE [workdayId] = @workdayId),
                                            [startDate] = (SELECT [startDate] FROM [TimeSignature] WHERE [workdayId] = @workdayId),
                                            [endDate] = (SELECT [endDate] FROM [TimeSignature] WHERE [workdayId] = @workdayId),
                                            [aom] = (SELECT DATEDIFF(MINUTE, @startDate, @endDate))
                                        WHERE [userId] = @userId 
                                        AND [id] = @workdayId
                                    ";
                                await _db.UpdateAsync(updateWorkdayById_string, updateWorkdayById_params);

                                IActionResult actionResult = customActionResult.Ok(@$"Your time signature was successfully updated.");
                                return StatusCode(StatusCodes.Status200OK, actionResult);
                            }

                            else
                            {
                                IActionResult actionResult = customActionResult.NotFound(
                                    @$"No time signatures found searching by workday id and user id."
                                );
                                return StatusCode(StatusCodes.Status404NotFound, actionResult);
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
