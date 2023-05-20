namespace Access.Models.Primitives
{
    public record struct TokenCode(string Value)
    {
        public override string ToString()
        {
            return Value;
        }

        public static implicit operator string(TokenCode tokenCode)
        {
            return tokenCode.Value;
        }

        public static implicit operator TokenCode(string tokenCode)
        {
            return new TokenCode(tokenCode);
        }
    }
}
