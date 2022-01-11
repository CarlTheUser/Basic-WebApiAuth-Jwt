namespace Access
{
    internal sealed class UserAccessDomain
    {
        public static void Require(Func<bool> invariant, string message)
        {
            if (!invariant.Invoke()) throw new UserAccessException(message);
        }
    }
}