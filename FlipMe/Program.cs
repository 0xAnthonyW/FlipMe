using System.DirectoryServices.AccountManagement;

string admin = "admin";

using (PrincipalContext context = new PrincipalContext(ContextType.Machine))
{
    UserPrincipal user = UserPrincipal.FindByIdentity(context, admin);

    if (user != null)
    {
        user.PasswordNeverExpires = true;
        user.Save();
    }
    else
    {
        Console.WriteLine("User not found");
    }
}