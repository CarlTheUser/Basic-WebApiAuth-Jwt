namespace Application.Authentication
{
    public class EmailPasswordAuthCredentials : IAuthCredentials
    {
        public string Email { get; }
        public char[] Password { get; }

        public EmailPasswordAuthCredentials(string email, string password)
        {
            Email = email;
            Password = password.ToCharArray();
        }

        public void Flush()
        {
            Array.Clear(Password, 0, Password.Length);
        }
    }
}
