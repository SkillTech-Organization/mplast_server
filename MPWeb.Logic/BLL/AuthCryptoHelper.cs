using System;
using System.Configuration;

namespace MPWeb.Logic.BLL
{
    /// <summary>
    /// Auth crypto helper class to abstract password management accross 
    /// </summary>
    public class AuthCryptoHelper
    {
        private string m_bcryptSalt;

        /// <summary>
        /// Create AuthCryptoHelper with default configuration. Application parameter AuthBCryptSalt must be set.
        /// </summary>
        public AuthCryptoHelper()
        {
            if (string.IsNullOrEmpty(ConfigurationManager.AppSettings["AuthBCryptSalt"]))
            {
                throw new Exception("ERROR: Application parameter AuthBCryptSalt is not set.");
            }
            m_bcryptSalt = ConfigurationManager.AppSettings["AuthBCryptSalt"];
        }
        
        /// <summary>
        /// Generate salt
        /// </summary>
        /// <returns>generated salt</returns>
        public string GenerateSalt()
        {
            return BCrypt.Net.BCrypt.GenerateSalt();
        }

        /// <summary>
        /// Hash password
        /// </summary>
        /// <param name="password">password to hash</param>
        /// <returns>hashed password</returns>
        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, m_bcryptSalt);
        }

        /// <summary>
        /// Verify password against stored hash.
        /// </summary>
        /// <param name="password">password to verify</param>
        /// <param name="hashedPassword">true if password matches its hash</param>
        /// <returns></returns>
        public bool VerifyPassword(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
    }
}
