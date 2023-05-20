using Data.Common.Contracts;

namespace Access.Events
{
    public class UserAccessCreatedDataEvent : DataEvent
    {
        public Guid User { get; }
        public string Email { get; }
        public Guid Role { get; }
        public byte[] Salt { get; }
        public byte[] Hash { get; }

        public UserAccessCreatedDataEvent(Guid user, string email, Guid role, byte[] salt, byte[] hash)
        {
            User = user;
            Email = email;
            Role = role;
            Salt = salt;
            Hash = hash;
        }
    }
}
