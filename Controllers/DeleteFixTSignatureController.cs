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
    public class DeleteFixTSignatureController : ControllerBase
    {
        private readonly ISqlDataAccess _db;
        private readonly IUserClaims _claims;

        private CustomActionResult customActionResult = new CustomActionResult();

        public DeleteFixTSignatureController(ISqlDataAccess db, IUserClaims claims)
        {
            _db = db;
            _claims = claims;
        }

        [Authorize]
        [HttpDelete("new/api/user/delete/wid={id}")]
        public async Task<IActionResult> DeleteAsOrdinaryAsync(int id, TimeSignatureModel body)
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
                    if (string.IsNullOrEmpty(body.typeCode) || string.IsNullOrWhiteSpace(body.typeCode))
                    {
                        IActionResult actionResult = customActionResult.FieldsRequired(
                            "The 'code type' field has been sent from the client null, empty, or whitespaced."
                        );
                        return StatusCode(StatusCodes.Status411LengthRequired, actionResult);
                    }

                    else if (body.typeCode != "FIX" && _claims.GetUserRole() != "admin")
                    {
                        IActionResult actionResult = customActionResult.Conflict(
                            "This option is only for 'Fixed Time Signature' type. If you want to delete a 'Flexible Time Signature', please contact your manager."
                        );
                        return StatusCode(StatusCodes.Status409Conflict, actionResult);
                    }

                    else if (body.typeCode == "FIX" && _claims.GetUserRole() != "admin")
                    {
                        DynamicParameters selectUserIdByWokrdayId_params = new DynamicParameters();
                        selectUserIdByWokrdayId_params.Add("@userId", _claims.GetUserId());
                        selectUserIdByWokrdayId_params.Add("@workdayId", id);

                        string selectUserIdByWokrdayId_string = $@"
                            SELECT [userId] 
                            FROM [Workday] 
                            WHERE [userId] = @userId 
                            AND [id] = @workdayId
                        ";

                        var selectUserIdByWorkdayId_result = await _db.SelectAsync<WorkdayModel, dynamic>(selectUserIdByWokrdayId_string, selectUserIdByWokrdayId_params);

                        if(selectUserIdByWorkdayId_result.Count < 1)
                        {
                            IActionResult actionResult = customActionResult.NotFound("No time signature found by given id");
                            return StatusCode(StatusCodes.Status404NotFound, actionResult);
                        }

                        else if(selectUserIdByWorkdayId_result[0].userId != _claims.GetUserId())
                        {
                            IActionResult actionResult = customActionResult.Unauthorized(
                                "You are not authorized to delete this time signature."
                            );
                            return StatusCode(StatusCodes.Status401Unauthorized, actionResult);
                        }

                        else
                        {
                            DynamicParameters deleteFixTSignatureByWorkdayId_params = new DynamicParameters();
                            deleteFixTSignatureByWorkdayId_params.Add("@workdayId", id);

                            string deleteFixTSignaturesByWorkdayId = $@"
                                DELETE FROM [TimeSignature] WHERE [workdayId] = @workdayId AND [typeCode] = 'FIX'
                            ";
                            await _db.DeleteAsync(deleteFixTSignaturesByWorkdayId, deleteFixTSignatureByWorkdayId_params);

                            string deleteFixWorkdayByWorkdayId = $@"
                                DELETE FROM [Workday] WHERE [id] = @workdayId
                            ";
                            await _db.DeleteAsync(deleteFixWorkdayByWorkdayId, deleteFixTSignatureByWorkdayId_params);

                            IActionResult actionResult = customActionResult.Ok(
                                "You deleted successfuly your workday."
                            );
                            return StatusCode(StatusCodes.Status200OK, actionResult);
                        }
                    }

                    else
                    {
                        DynamicParameters selectUserIdByWokrdayId_params = new DynamicParameters();
                        selectUserIdByWokrdayId_params.Add("@userId", _claims.GetUserId());
                        selectUserIdByWokrdayId_params.Add("@workdayId", id);

                        string selectUserIdByWokrdayId_string = $@"
                            SELECT [userId] 
                            FROM [Workday] 
                            WHERE [userId] = @userId 
                            AND [id] = @workdayId
                        ";

                        var selectUserIdByWorkdayId_result = await _db.SelectAsync<WorkdayModel, dynamic>(selectUserIdByWokrdayId_string, selectUserIdByWokrdayId_params);

                        if (selectUserIdByWorkdayId_result.Count < 1)
                        {
                            IActionResult actionResult = customActionResult.NotFound("No time signature found by given id");
                            return StatusCode(StatusCodes.Status404NotFound, actionResult);
                        }

                        else if (selectUserIdByWorkdayId_result[0].userId != _claims.GetUserId())
                        {
                            IActionResult actionResult = customActionResult.Unauthorized(
                                "You are not authorized to delete this time signature."
                            );
                            return StatusCode(StatusCodes.Status401Unauthorized, actionResult);
                        }

                        else
                        {
                            DynamicParameters deleteFixTSignatureByWorkdayId_params = new DynamicParameters();
                            deleteFixTSignatureByWorkdayId_params.Add("@workdayId", id);

                            string deleteFixTSignaturesByWorkdayId = $@"
                                DELETE FROM [TimeSignature] WHERE [workdayId] = @workdayId
                            ";
                            await _db.DeleteAsync(deleteFixTSignaturesByWorkdayId, deleteFixTSignatureByWorkdayId_params);

                            string deleteFixWorkdayByWorkdayId = $@"
                                DELETE FROM [Workday] WHERE [id] = @workdayId
                            ";
                            await _db.DeleteAsync(deleteFixWorkdayByWorkdayId, deleteFixTSignatureByWorkdayId_params);

                            IActionResult actionResult = customActionResult.Ok(
                                "You deleted successfuly your workday."
                            );
                            return StatusCode(StatusCodes.Status200OK, actionResult);
                        }
                    }
                }
            }

            catch(Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        [Authorize(Policy = "AdminRolePolicy")]
        [HttpDelete("new/api/user/admin/delete/wid={id}")]
        public async Task<IActionResult> DeleteAsAdminAsync(int id, TimeSignatureModel body)
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
                    if (string.IsNullOrEmpty(body.typeCode) || string.IsNullOrWhiteSpace(body.typeCode))
                    {
                        IActionResult actionResult = customActionResult.FieldsRequired(
                            "The 'code type' field has been sent from the client null, empty, or whitespaced."
                        );
                        return StatusCode(StatusCodes.Status411LengthRequired, actionResult);
                    }

                    else if (_claims.GetUserRole() != "admin")
                    {
                        IActionResult actionResult = customActionResult.Unauthorized(
                            "You are not authorized to access this route."
                        );
                        return StatusCode(StatusCodes.Status401Unauthorized, actionResult);
                    }

                    else
                    {
                        DynamicParameters selectUserIdByWokrdayId_params = new DynamicParameters();
                        selectUserIdByWokrdayId_params.Add("@workdayId", id);

                        string selectUserIdByWokrdayId_string = $@"
                            SELECT [userId] 
                            FROM [Workday] 
                            WHERE [id] = @workdayId
                        ";

                        var selectUserIdByWorkdayId_result = await _db.SelectAsync<WorkdayModel, dynamic>(selectUserIdByWokrdayId_string, selectUserIdByWokrdayId_params);

                        if(selectUserIdByWorkdayId_result.Count < 1)
                        {
                            IActionResult actionResult = customActionResult.NotFound("No time signature found by given id");
                            return StatusCode(StatusCodes.Status404NotFound, actionResult);
                        }

                        else
                        {
                            DynamicParameters deleteFixTSignatureByWorkdayId_params = new DynamicParameters();
                            deleteFixTSignatureByWorkdayId_params.Add("@workdayId", id);

                            string deleteFixTSignaturesByWorkdayId = $@"
                                DELETE FROM [TimeSignature] WHERE [WorkdayId] = @workdayId
                            ";
                            await _db.DeleteAsync(deleteFixTSignaturesByWorkdayId, deleteFixTSignatureByWorkdayId_params);

                            string deleteFixWorkdayByWorkdayId = $@"
                                DELETE FROM [Workday] WHERE [id] = @workdayId
                            ";
                            await _db.DeleteAsync(deleteFixWorkdayByWorkdayId, deleteFixTSignatureByWorkdayId_params);

                            IActionResult actionResult = customActionResult.Ok(
                                "You deleted successfuly your workday."
                            );
                            return StatusCode(StatusCodes.Status200OK, actionResult);
                        }
                        
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
