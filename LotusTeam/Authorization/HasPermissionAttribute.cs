using Microsoft.AspNetCore.Authorization;

namespace LotusTeam.Authorization
{
    public class HasPermissionAttribute : AuthorizeAttribute
    {
        public HasPermissionAttribute(string permission)
        {
            Policy = $"PERMISSION_{permission}";
        }
    }
}
