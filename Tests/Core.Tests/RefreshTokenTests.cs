using Access;
using Access.Events;
using Access.Models.Entities;
using Access.Models.Primitives;
using Data.Common.Contracts;

namespace Core.Tests
{
    [TestClass]
    public class RefreshTokenTests
    {
        [TestMethod]
        public void Consume_IsAlreadyConsumed_ThrowsException()
        {
            var refreshTokenId = new RefreshTokenId(Guid.NewGuid());
            var userAccessId = new UserAccessId(Guid.NewGuid());
            var tokenCode = new TokenCode("Abc123");
            var issued = DateTime.Now;
            var expiry = issued.AddHours(1);

            var refreshToken = new RefreshToken(
                id: refreshTokenId,
                issuedTo: userAccessId,
                code: tokenCode,
                issued: issued,
                expiry: expiry);

            refreshToken.Consume();

            Assert.ThrowsException<UserAccessException>(() => refreshToken.Consume());
        }

        [TestMethod]
        public void Consume_Expired_ThrowsException()
        {
            var refreshTokenId = new RefreshTokenId(Guid.NewGuid());
            var userAccessId = new UserAccessId(Guid.NewGuid());
            var tokenCode = new TokenCode("Abc123");
            var issued = DateTime.Now.AddHours(-1);
            var expiry = issued.AddMinutes(20);

            var refreshToken = new RefreshToken(
                id: refreshTokenId,
                issuedTo: userAccessId,
                code: tokenCode,
                issued: issued,
                expiry: expiry);

            Assert.ThrowsException<UserAccessException>(() => refreshToken.Consume());
        }

        [TestMethod]
        public void Consume_NotIsConsumedAndNotExpired_NoExceptionThrown()
        {
            var refreshTokenId = new RefreshTokenId(Guid.NewGuid());
            var userAccessId = new UserAccessId(Guid.NewGuid());
            var tokenCode = new TokenCode("Abc123");
            var issued = DateTime.Now;
            var expiry = issued.AddHours(1);

            var refreshToken = new RefreshToken(
                id: refreshTokenId,
                issuedTo: userAccessId,
                code: tokenCode,
                issued: issued,
                expiry: expiry);

            refreshToken.Consume();
        }

        [TestMethod]
        public void DequeueDataEvent_Consumed_ContainsRefreshTokenConsumedDataEvent()
        {
            var refreshTokenId = new RefreshTokenId(Guid.NewGuid());
            var userAccessId = new UserAccessId(Guid.NewGuid());
            var tokenCode = new TokenCode("Abc123");
            var issued = DateTime.Now;
            var expiry = issued.AddHours(1);

            var refreshToken = new RefreshToken(
                id: refreshTokenId,
                issuedTo: userAccessId,
                code: tokenCode,
                issued: issued,
                expiry: expiry);

            refreshToken.Consume();

            bool hasRefreshTokenConsumedDataEvent = false;

            DataEvent? dataEvent;

            while ((dataEvent = refreshToken.DequeueDataEvent()) != null)
            {
                if(dataEvent is RefreshTokenConsumedDataEvent)
                {
                    hasRefreshTokenConsumedDataEvent = true; 
                    break;
                }
            }

            Assert.IsTrue(hasRefreshTokenConsumedDataEvent);
        }
    }
}