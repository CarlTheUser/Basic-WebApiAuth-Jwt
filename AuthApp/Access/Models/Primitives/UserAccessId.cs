namespace Access.Models.Primitives
{
    public record struct UserAccessId(Guid Value)
    {
        public override string ToString()
        {
            return Value.ToString();
        }

        public static implicit operator Guid(UserAccessId userAccessId)
        {
            return userAccessId.Value;
        }

        public static implicit operator UserAccessId(Guid guid)
        {
            return new UserAccessId(guid);
        }
    }
}
