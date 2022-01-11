using Data.Common.Contracts;

namespace Access
{
    public class PasswordChangedDataEvent : DataEvent
    {
        public Guid User { get; }
        public byte[] Salt { get; }
        public byte[] Hash { get; }

        public PasswordChangedDataEvent(Guid user, byte[] salt, byte[] hash)
        {
            User = user;
            Salt = salt;
            Hash = hash;
        }
    }
}