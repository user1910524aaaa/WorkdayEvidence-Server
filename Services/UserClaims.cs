using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Server.Services
{

    public class UserClaims : IUserClaims
    {

        private readonly IHttpContextAccessor httpContextAccessor;

        public UserClaims(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }


        private Claim GetSidClaim()
            => httpContextAccessor.HttpContext.User.FindFirst(c => c.Type == ClaimTypes.Sid);

        private Claim GetRoleClaim()
            => httpContextAccessor.HttpContext.User.FindFirst(c => c.Type == ClaimTypes.Role);

        private Claim GetNameClaim()
            => httpContextAccessor.HttpContext.User.FindFirst(c => c.Type == ClaimTypes.Name);


        public int GetUserId() => Convert.ToInt32(GetSidClaim().Value.ToString());

        public string GetUserRole() => GetRoleClaim().Value;

        public string GetUserName() => GetNameClaim().Value;
    }
}