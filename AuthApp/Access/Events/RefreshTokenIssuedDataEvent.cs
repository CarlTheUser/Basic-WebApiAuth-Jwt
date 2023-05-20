using Access.Models.Primitives;
using Data.Common.Contracts;

namespace Access.Events
{
    public class RefreshTokenIssuedDataEvent : DataEvent
    {
        public Guid RefreshTokenId { get; }
        public Guid IssuedTo { get; }
        public string TokenCode { get; }
        public DateTime Issued { get; }
        public DateTime Expiry { get; }

        public RefreshTokenIssuedDataEvent(RefreshTokenId id, UserAccessId issuedTo, TokenCode tokenCode, DateTime issued, DateTime expiry)
        {
            RefreshTokenId = id.Value;
            IssuedTo = issuedTo.Value;
            TokenCode = tokenCode.Value;
            Issued = issued;
            Expiry = expiry;
        }
    }
}
