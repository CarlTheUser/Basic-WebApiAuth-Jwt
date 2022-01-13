using Data.Common.Contracts;

namespace Application
{
    public class RefreshTokenIssuedDataEvent : DataEvent
    {
        public Guid Id { get; }
        public Guid IssuedTo { get; }
        public string Value { get; }
        public DateTime Issued { get; }
        public DateTime Expiry { get; }

        public RefreshTokenIssuedDataEvent(Guid id, Guid issuedTo, string value, DateTime issued, DateTime expiry)
        {
            Id = id;
            IssuedTo = issuedTo;
            Value = value;
            Issued = issued;
            Expiry = expiry;
        }
    }
}
