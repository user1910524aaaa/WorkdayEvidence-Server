namespace Server.Data
{
    public class UserAccountModel
    {

        public int id { get; set; }

        public string firstName { get; set; }

        public string lastName { get; set; }

        public string userName { get; set; }

        public string role { get; set; }

        public string password { get; set; }

        public string oldPassword { get; set; }

    }
}