// See https://aka.ms/new-console-template for more information
using Access;
using Data.Sql.Provider;

Console.WriteLine("Hello, World!");

static void InitializeUserAccess()
{
    SqlServerProvider sqlServerProvider = new SqlServerProvider(@"Data Source='DESKTOP-U64U4KB\SQLEXPRESS';Initial Catalog=UserAccessManagementDb;User ID=sa;Password=password;");

    UserAccess userAccess = UserAccess.New("qazzqc@foo.company", )
}