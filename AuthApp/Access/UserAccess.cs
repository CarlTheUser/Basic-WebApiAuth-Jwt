using Data.Common.Contracts;

namespace Access
{

    public class UserAccess : IEventStore
    {
        public static UserAccess New(string email, Role role, SecurePassword password)
        {
            UserAccess user = new (Guid.NewGuid(), email, role, password);

            user._events.Add(
                new UserAccessCreatedDataEvent(
                    user.Guid,
                    user.Email,
                    user.Role.Guid,
                    user.Password.Salt,
                    user.Password.Value));

            return user;
        }

        public static UserAccess Existing(Guid guid, string email, Role role, SecurePassword password)
        {
            return new UserAccess(guid, email, role, password);
        }

        public Guid Guid { get; }
        public string Email { get; }
        public Role Role { get; private set; }
        public SecurePassword Password { get; private set; }

        private readonly List<DataEvent> _events = new();

        private UserAccess(Guid guid, string email, Role role, SecurePassword password)
        {
            Guid = guid;
            Email = email;
            Role = role;
            Password = password;
        }

        public void ChangePassword(SecurePassword newPassword)
        {
            Password = newPassword;
            _events.Add(new PasswordChangedDataEvent(Guid, Password.Salt, Password.Value));
        }

        public void ChangeRole(Role newRole)
        {
            UserAccessDomain.Require(() => newRole.Name != Role.Name, "Cannot apply the same role for user.");
            Role = newRole;
            _events.Add(new RoleChangedDataEvent(Guid, Role.Guid));
        }

        public IEnumerable<DataEvent> ReleaseEvents()
        {
            IReadOnlyList<DataEvent> copy = _events.ToList();

            _events.Clear();

            return copy;
        }
    }
}