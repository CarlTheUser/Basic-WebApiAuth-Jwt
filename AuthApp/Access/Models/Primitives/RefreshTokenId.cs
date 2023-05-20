namespace Access.Models.Primitives
{
    public record struct RefreshTokenId(Guid Value)
    {
        public override string ToString()
        {
            return Value.ToString();
        }

        public static implicit operator Guid(RefreshTokenId refreshTokenId)
        {
            return refreshTokenId.Value;
        }
    }
}
