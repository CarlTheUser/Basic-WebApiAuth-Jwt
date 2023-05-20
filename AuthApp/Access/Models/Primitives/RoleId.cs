namespace Access.Models.Primitives
{
    public record struct RoleId(Guid Value)
    {
        public override string ToString()
        {
            return Value.ToString();
        }

        public static implicit operator Guid(RoleId roleId)
        {
            return roleId.Value;
        }
    }
}
