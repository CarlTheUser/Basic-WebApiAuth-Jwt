using Access.Base;
using Access.Events;
using Access.Models.Primitives;
using Access.Models.ValueObjects;
using Data.Common.Contracts;

namespace Access.Models.Entities
{
    public class UserAccess : AggregateRoot
    {
        public static UserAccess New(Email email, RoleId roleId, SecurePassword password)
        {
            UserAccess user = new(
                userAccessId: new UserAccessId(Guid.NewGuid()),
                email: email,
                roleId: roleId, 
                password: password);

            user._dataEvents.Enqueue(
                new UserAccessCreatedDataEvent(
                    user: user.Id,
                    email: user.Email.Value,
                    role: user.RoleId,
                    salt: user.Password.Salt,
                    hash: user.Password.Value));

            return user;
        }

        public static UserAccess Existing(UserAccessId userAccessId, Email email, RoleId roleId, SecurePassword password) => new(
            userAccessId: userAccessId,
            email: email,
            roleId: roleId,
            password: password);

        public UserAccessId Id { get; }
        public Email Email { get; }
        public RoleId RoleId { get; private set; }
        public SecurePassword Password { get; private set; }

        private readonly Queue<DataEvent> _dataEvents = new();

        private UserAccess(UserAccessId userAccessId, Email email, RoleId roleId, SecurePassword password)
        {
            Id = userAccessId;
            Email = email;
            RoleId = roleId;
            Password = password;
        }

        public void ChangePassword(SecurePassword newPassword)
        {
            Password = newPassword;
            _dataEvents.Enqueue(new PasswordChangedDataEvent(Id, Password.Salt, Password.Value));
        }

        public void ChangeRole(RoleId newRole)
        {
            RoleId = newRole;
            _dataEvents.Enqueue(new RoleChangedDataEvent(Id, RoleId));
        }

        public DataEvent? DequeueDataEvent()
        {
            if (_dataEvents.Count == 0)
            {
                return null;
            }

            return _dataEvents.Dequeue();
        }
    }
}