using Access.Events;
using Access.Models.Entities;
using Access.Models.Primitives;
using Access.Models.ValueObjects;
using Data.Common.Contracts;

namespace Core.Tests
{
    [TestClass]
    public class UserAccessTests
    {
        [TestMethod]
        public void DequeueDataEvent_Should_Contain_UserAccessCreatedDataEvent_From_Newly_Created_UserAccess()
        {
            var email = new Email("theMail@provider.com");
            var roleId = new RoleId(Guid.NewGuid());
            var oldSecurePassword = new SecurePassword("Old Password");

            var userAccess = UserAccess.New(
                email: email,
                roleId: roleId,
                password: oldSecurePassword);

            bool hasUserAccessCreatedDataEvent = false;

            DataEvent? dataEvent;

            while ((dataEvent = userAccess.DequeueDataEvent()) != null)
            {
                if (dataEvent is UserAccessCreatedDataEvent)
                {
                    hasUserAccessCreatedDataEvent = true;
                    break;
                }
            }

            Assert.IsTrue(hasUserAccessCreatedDataEvent);
        }

        [TestMethod]
        public void ChangePassword_NewPassword_EqualPassword()
        {
            var email = new Email("theMail@provider.com");
            var roleId = new RoleId(Guid.NewGuid());
            var oldSecurePassword = new SecurePassword("Old Password");

            var userAccess = UserAccess.New(
                email: email,
                roleId: roleId,
                password: oldSecurePassword);

            var newSecurePassword = new SecurePassword("New Password");

            userAccess.ChangePassword(newSecurePassword);

            Assert.AreEqual(newSecurePassword, userAccess.Password);
        }

        [TestMethod]
        public void DequeueDataEvent_Should_Contain_PasswordChangedDataEvent_After_Changing_Password()
        {
            var email = new Email("theMail@provider.com");
            var roleId = new RoleId(Guid.NewGuid());
            var oldSecurePassword = new SecurePassword("Old Password");

            var userAccess = UserAccess.New(
                email: email,
                roleId: roleId,
                password: oldSecurePassword);

            var newSecurePassword = new SecurePassword("New Password");

            userAccess.ChangePassword(newSecurePassword);

            bool hasPasswordChangedDataEvent = false;

            DataEvent? dataEvent;

            while ((dataEvent = userAccess.DequeueDataEvent()) != null)
            {
                if (dataEvent is PasswordChangedDataEvent)
                {
                    hasPasswordChangedDataEvent = true;
                    break;
                }
            }

            Assert.IsTrue(hasPasswordChangedDataEvent);
        }

        [TestMethod]
        public void ChangeRole_NewRole_EqualRole()
        {
            var email = new Email("theMail@provider.com");
            var roleId = new RoleId(Guid.NewGuid());
            var securePassword = new SecurePassword("Old Password");

            var newRoleId = new RoleId(Guid.NewGuid());

            var userAccess = UserAccess.New(
                email: email,
                roleId: roleId,
                password: securePassword);


            userAccess.ChangeRole(newRoleId);

            Assert.AreEqual(newRoleId, userAccess.RoleId);
        }
    }
}