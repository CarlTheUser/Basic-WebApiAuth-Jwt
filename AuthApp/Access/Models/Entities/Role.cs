using Access.Base;
using Access.Models.Primitives;

namespace Access.Models.Entities
{
    public class Role : Entity
    {
        public RoleId RoleId { get; }
        public string Name { get; }

        public Role(RoleId roleId, string name)
        {
            RoleId = roleId;
            Name = name;
        }
    }
}