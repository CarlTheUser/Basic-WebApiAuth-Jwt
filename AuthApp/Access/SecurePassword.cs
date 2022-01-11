using System.Security.Cryptography;


namespace Access
{
    public class SecurePassword : IDisposable
    {
        #region Static Constants

        private static readonly int SALT_SIZE = 16;

        private static readonly int HASH_SIZE = 20;

        private static readonly int ITERATIONS = 10000;

        #endregion

        #region Internal Functions

        private static byte[] CalculateHash(byte[] salt, string password)
        {
            DeriveBytes encryptor = new Rfc2898DeriveBytes(password, salt, ITERATIONS);
            byte[] hash = encryptor.GetBytes(HASH_SIZE);
            byte[] hashedPassword = new byte[SALT_SIZE + HASH_SIZE];
            Array.Copy(salt, 0, hashedPassword, 0, SALT_SIZE);
            Array.Copy(hash, 0, hashedPassword, SALT_SIZE, HASH_SIZE);

            return hashedPassword;
        }

        private static byte[] CreateSalt(RandomNumberGenerator rng, int size)
        {
            using (rng)
            {
                byte[] bytes = new byte[size];
                rng.GetBytes(bytes);
                return bytes;
            }
        }

        #endregion

        public byte[] Salt { get; private set; }

        public byte[] Value { get; private set; }

        #region Constructors

        public SecurePassword(string peanuts, string password)
        {
            UserAccessDomain.Require(() => !string.IsNullOrWhiteSpace(password), "Cannot create an empty password.");

            Salt = CreateSalt(new RNGCryptoServiceProvider(), SALT_SIZE);
            Value = CalculateHash(Salt, peanuts + password);
        }

        public SecurePassword(byte[] salt, byte[] value)
        {
            Salt = salt;
            Value = value;
        }

        #endregion

        public bool Test(string password)
        {
            using (SecurePassword testConverted = new(Salt.ToArray(), CalculateHash(Salt, password)))
            {
                bool mismatched = false;

                int size = HASH_SIZE;

                byte[] hashedPassword = Value;

                byte[] testPassword = testConverted.Value;

                for (int i = 0; i < size; ++i)
                {
                    if (testPassword[i] != hashedPassword[i])
                    {
                        mismatched = true;
                        break;
                    }
                }

                return !mismatched;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Array.Clear(Salt, 0, Salt.Length);
                Array.Clear(Value, 0, Value.Length);
            }
        }
    }
}