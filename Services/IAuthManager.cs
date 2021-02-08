using System;
using System.Linq;
using System.Text;

namespace Server.Services
{

    public interface IAuthManager
    {

        String GenerateJSONWebToken(Data.UserAccountModel body, Data.UserAccountModel user);

    }
}