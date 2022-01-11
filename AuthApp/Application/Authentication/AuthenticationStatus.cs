namespace Application.Authentication
{
    public enum AuthenticationStatus : int
    {
        Ok,
        NotFound,
        InvalidCredentials,
        Locked,
        Deactivated
    }

    
}
