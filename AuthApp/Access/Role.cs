namespace Access
{
    public class Role
    {
        public Guid Guid { get; }
        public string Name { get; }

        public Role(Guid guid, string name)
        {
            Guid = guid;
            Name = name;
        }
    }
}