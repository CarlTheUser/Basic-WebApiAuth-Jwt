namespace Access.Models.Primitives
{
    public record class Email
    {
        public string Value { get; }

        public Email(string value)
        {
            UserAccessDomain.Require(
                invariant: () => !string.IsNullOrWhiteSpace(value),
                message: "Unable to construct email from an empty or whitespace string.");

            Value = value;
        }

        public static implicit operator Email(string email)
        {
            return new Email(email.Trim());
        }

        public override string ToString()
        {
            return Value;
        }
    }
}
