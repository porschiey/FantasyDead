namespace FantasyDead.Crypto
{
    using System;

    public class Cryptographer
    {

        private readonly SimpleAES aes;

        public Cryptographer()
        {
            this.aes = new SimpleAES();
        }

        /// <summary>
        /// Encrypts a string.
        /// </summary>
        /// <param name="clearText"></param>
        /// <returns></returns>
        public string Encrypt(string clearText)
        {
            try
            {
                return this.aes.EncryptToString(clearText);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Decrypts a string.
        /// </summary>
        /// <param name="cipher"></param>
        /// <returns></returns>
        public string Decrypt(string cipher)
        {
            try
            {
                return this.aes.DecryptString(cipher);
            }
            catch
            {
                return null;
            }
        }


        /// <summary>
        /// Creates a token for access to the system.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="username"></param>
        /// <param name="role"></param>
        /// <param name="expireInDays"></param>
        /// <returns></returns>
        public string CreateToken(string id, string username, int role, int expireInDays = 7)
        {
            var expiration = DateTime.UtcNow.AddDays(expireInDays);
            var scaffolding = $"{id}|{Guid.NewGuid()}|{username}|{role}|{expiration}";
            var token = this.Encrypt(scaffolding);

            return token;
        }

        /// <summary>
        /// Reads given token in string form and provides a workable object representing the token.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public BearerToken DecipherToken(string token)
        {
            var rawToken = this.Decrypt(token);

            if (rawToken == null)
                return null;

            var parts = rawToken.Split('|');

            var bToken = new BearerToken
            {
                RawToken = token,
                PersonId = parts[0],
                Username = parts[2],
                Role = Convert.ToInt32(parts[3]),
                Expiration = DateTime.Parse(parts[4])
            };

            return bToken;
        }
    }
}
