using System.DirectoryServices.AccountManagement;

using (PrincipalContext context = new PrincipalContext(ContextType.Machine))
{
    UserPrincipal user = UserPrincipal.FindByIdentity(context, "admin");

    if (user != null)
    {
        user.PasswordNeverExpires = true;
        user.Save();
    }
}