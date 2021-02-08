using System;
using System.Text;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;


namespace Server.Controllers
{

    public class HashPassword
    {

        public byte[] CreateSalt(int size)
        {
            var rng = new RNGCryptoServiceProvider();
            var buff = new byte[size];
            rng.GetBytes(buff);

            return buff;
        }

        public string GenerateSHA256Hash(string password, byte[] salt, bool needsOnlyHash)
        {

            byte[] bytes = Encoding.UTF8.GetBytes(password + salt);
            SHA256Managed Sha256HashString = new SHA256Managed();
            Byte[] hash = Sha256HashString.ComputeHash(bytes);
            var hashPassword = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 256/8
            ));

            if (needsOnlyHash) 
                return hashPassword;

            // password will be concatenated with salt using ':'
            return $"{hashPassword}.{Convert.ToBase64String(salt)}";
        }

        public Boolean VerifyPassword(string hashedPasswordWithSalt, string passwordToCheck)
        {
            // retrieve both salt and password from 'hashedPasswordWithSalt'
            var passwordAndHash = hashedPasswordWithSalt.Split('.');
            if (passwordAndHash == null || passwordAndHash.Length != 2)
                return false;

            var salt = Convert.FromBase64String(passwordAndHash[1]);
            if (salt == null)
                return false;

            // hash the given password
            var hashOfpasswordToCheck = GenerateSHA256Hash(passwordToCheck, salt, true);
            // compare both hashes
            if (string.Compare(passwordAndHash[0], hashOfpasswordToCheck) == 0)
                return true;

            return false;
        }

    }
}