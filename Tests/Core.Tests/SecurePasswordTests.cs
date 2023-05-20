using Access.Models.ValueObjects;

namespace Core.Tests
{
    [TestClass]
    public class SecurePasswordTests
    {
        [TestMethod]
        public void Test_MatchingPasswords_ReturnsTrue()
        {
            string passwordString = "If you need a passphrase for a specific purpose, I would recommend consulting the guidelines and requirements for that purpose to ensure that your passphrase meets the necessary strength and complexity requirements.";
            var securePassword = new SecurePassword(passwordString);

            var result = securePassword.Test(passwordString);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void Test_NonMatchingPasswords_ReturnsFalse()
        {
            string passwordString = "If you need a passphrase for a specific purpose, I would recommend consulting the guidelines and requirements for that purpose to ensure that your passphrase meets the necessary strength and complexity requirements.";
            var securePassword = new SecurePassword("In general, longer passphrases are stronger than shorter ones, so a 15-word passphrase is quite strong in terms of its length. However, it's still important to use a mix of character types (e.g., uppercase letters, lowercase letters, numbers, and special characters) to make the passphrase more difficult to guess or brute-force.");

            var result = securePassword.Test(passwordString);

            Assert.IsFalse(result);
        }
    }
}