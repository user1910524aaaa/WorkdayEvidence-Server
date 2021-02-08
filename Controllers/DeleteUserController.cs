using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Server.Data;
using Server.Services;

namespace Server.Controllers
{
    [ApiController]
    public class DeleteUserController : ControllerBase
    {
        private readonly ISqlDataAccess _db;
        private readonly IUserClaims _claims;

        private CustomActionResult customActionResult = new CustomActionResult();

        public DeleteUserController(ISqlDataAccess db, IUserClaims claims)
        {
            _db = db;
            _claims = claims;
        }

        [Authorize(Policy = "AdminRolePolicy")]
        [HttpDelete("new/api/user/admin/delete/uid={id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                DynamicParameters selectUserById_params = new DynamicParameters();
                selectUserById_params.Add("@userId", id);

                string selectUserById_string = $@"SELECT [userName] FROM [User] WHERE [id] = @userId";

                var selectUserById_result = await _db.SelectAsync<UserAccountModel, dynamic>(selectUserById_string, selectUserById_params);

                if(selectUserById_result.Count < 1)
                {
                    IActionResult actionResult = customActionResult.NotFound("No user found by given id");
                    return StatusCode(StatusCodes.Status404NotFound, actionResult);
                }

                else
                {
                    DynamicParameters deleteUserById_params = new DynamicParameters();
                    deleteUserById_params.Add("@userId", id);

                    string deleteUserById_string = $@"DELETE FROM [User] WHERE [id] = @userId";

                    await _db.DeleteAsync(deleteUserById_string, deleteUserById_params);

                    IActionResult actionResult = customActionResult.Ok(
                        "You successfuly deleted user account."
                    );
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
