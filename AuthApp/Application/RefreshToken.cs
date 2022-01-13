using Data.Common.Contracts;
using Misc.Utilities;

namespace Application
{
    public class RefreshToken : IEventStore
    {
        public static RefreshToken For(Guid user, TimeSpan lifespan, IRandomStringGenerator generator, int tokenLength)
        {
            DateTime now = DateTime.Now;

            RefreshToken token = new(
                id: Guid.NewGuid(),
                issuedTo: user,
                value: generator.Generate(tokenLength),
                issued: now,
                expiry: now.Add(lifespan));

            token._events.Add(
                new RefreshTokenIssuedDataEvent(
                    id: token.Id,
                    issuedTo: token.IssuedTo,
                    value: token.Value,
                    issued: token.Issued,
                    expiry: token.Expiry));

            return token;
        }

        public Guid Id { get; }
        public Guid IssuedTo { get; }
        public string Value { get; }
        public DateTime Issued { get; }
        public DateTime Expiry { get; }

        private readonly List<DataEvent> _events = new List<DataEvent>();

        private bool _isConsumed = false;

        public RefreshToken(Guid id, Guid issuedTo, string value, DateTime issued, DateTime expiry)
        {
            if(expiry < DateTime.Now)
            {
                throw new ApplicationException($"Unable to create instance of {typeof(RefreshToken).Name} with {nameof(expiry)} of past date.");
            }

            Id = id;
            IssuedTo = issuedTo;
            Value = value;
            Issued = issued;
            Expiry = expiry;
        }

        public void Consume()
        {
            if (_isConsumed)
            {
                throw new ApplicationException("Cannot consume this token more than once.");
            }
            _isConsumed = true;

            _events.Add(new RefreshTokenConsumedDataEvent(Id));
        }

        public IEnumerable<DataEvent> ReleaseEvents()
        {
            IReadOnlyList<DataEvent> copy = _events.ToList();

            _events.Clear();

            return copy;
        }
    }
}
