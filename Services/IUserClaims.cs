namespace Server.Services
{
    public interface IUserClaims
    {
        int GetUserId();
        string GetUserName();
        string GetUserRole();
    }
}