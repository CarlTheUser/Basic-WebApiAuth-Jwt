using Access.Base;
using Access.Events;
using Access.Models.Primitives;
using Data.Common.Contracts;
using Misc.Utilities;

namespace Access.Models.Entities
{
    public class RefreshToken : AggregateRoot
    {
        #region Static Factory Methods

        /// <summary>
        /// Static factory method for creating a new <code>RefreshToken</code> object for a specified user.
        /// </summary>
        /// <param name="userAccess"></param>
        /// <param name="lifespan"></param>
        /// <param name="generator"></param>
        /// <param name="tokenLength"></param>
        /// <returns></returns>
        public static RefreshToken For(UserAccessId userAccess, TimeSpan lifespan, IRandomStringGenerator generator, int tokenLength)
        {
            DateTime now = DateTime.Now;

            RefreshToken token = new(
                id: new RefreshTokenId(Value: Guid.NewGuid()),
                issuedTo: userAccess,
                code: new TokenCode(Value: generator.Generate(tokenLength)),
                issued: now,
                expiry: now.Add(lifespan));

            token._dataEvents.Enqueue(
                new RefreshTokenIssuedDataEvent(
                    id: token.Id,
                    issuedTo: token.IssuedTo,
                    tokenCode: token.Code,
                    issued: token.Issued,
                    expiry: token.Expiry));

            return token;
        }

        #endregion

        public RefreshTokenId Id { get; }
        public UserAccessId IssuedTo { get; }
        public TokenCode Code { get; }
        public DateTime Issued { get; }
        public DateTime Expiry { get; }

        private readonly Queue<DataEvent> _dataEvents = new();

        private bool _isConsumed = false;

        public RefreshToken(RefreshTokenId id, UserAccessId issuedTo, TokenCode code, DateTime issued, DateTime expiry)
        {
            Id = id;
            IssuedTo = issuedTo;
            Code = code;
            Issued = issued;
            Expiry = expiry;
        }

        public void Consume()
        {
            UserAccessDomain.Require(
                invariant: () => !_isConsumed,
                message: "Cannot consume this token more than once.");

            UserAccessDomain.Require(
               invariant: () => Expiry > DateTime.Now,
               message: "Cannot consume expired token.");

            _isConsumed = true;

            _dataEvents.Enqueue(new RefreshTokenConsumedDataEvent(refreshTokenId: Id));
        }

        public DataEvent? DequeueDataEvent()
        {
            if (_dataEvents.Count == 0)
            {
                return null;
            }

            return _dataEvents.Dequeue();
        }

        public override string ToString()
        {
            return Code.Value;
        }
    }
}
