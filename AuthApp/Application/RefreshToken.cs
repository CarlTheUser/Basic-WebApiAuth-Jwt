namespace Application
{
    public class RefreshToken
    {
        public static RefreshToken For(Guid user, TimeSpan lifespan)
        {
            DateTime now = DateTime.Now;

            return new RefreshToken(
                id: Guid.NewGuid(),
                issuedTo: user,
                value: "",
                issued: now,
                expiry: now.Add(lifespan));
        }

        public Guid Id { get; }
        public Guid IssuedTo { get; }
        public string Value { get; }
        public DateTime Issued { get; }
        public DateTime Expiry { get; }

        public RefreshToken(Guid id, Guid issuedTo, string value, DateTime issued, DateTime expiry)
        {
            Id = id;
            IssuedTo = issuedTo;
            Value = value;
            Issued = issued;
            Expiry = expiry;
        }
    }
}
