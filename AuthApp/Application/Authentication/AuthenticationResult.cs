namespace Application.Authentication
{
    public abstract class AuthenticationResult
    {
        public static readonly AuthenticationResult NotFound = new NotFoundResult();

        public static AuthenticationResult Ok(AuthenticatedUser user) => new OkResult(user);

        public static AuthenticationResult Deactivated() => new DeactivatedResult();

        public static AuthenticationResult InvalidCredentials(string identifier) => new InvalidCredentialsResult(identifier);

        public AuthenticationStatus Status { get; }

        protected AuthenticationResult(AuthenticationStatus status)
        {
            Status = status;
        }
    }

    public class OkResult : AuthenticationResult
    {
        public AuthenticatedUser  User { get; }

        public OkResult(AuthenticatedUser user) : base(status: AuthenticationStatus.Ok) { User = user; }
    }

    public class NotFoundResult : AuthenticationResult
    {
        public NotFoundResult() : base(status: AuthenticationStatus.NotFound) { }
    }

    public class DeactivatedResult : AuthenticationResult
    {
        public DeactivatedResult() : base(status: AuthenticationStatus.Deactivated) { }
    }

    public class InvalidCredentialsResult : AuthenticationResult
    {
        // User might sign in using phone number, email, etc. Formal identifier may be username
        public string FormalIdentifier { get; }

        public InvalidCredentialsResult(string identifier) : base(status: AuthenticationStatus.InvalidCredentials) { FormalIdentifier = identifier; }
    }
}
