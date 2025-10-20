namespace AppBackend.BusinessObjects.Dtos
{
    public class RoleDto
    {
        public int RoleId { get; set; }
        public string RoleValue { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateRoleRequest
    {
        public string RoleValue { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class UpdateRoleRequest
    {
        public string? RoleName { get; set; }
        public string? Description { get; set; }
        public bool? IsActive { get; set; }
    }

    public class AssignRolesRequest
    {
        public int AccountId { get; set; }
        public List<int> RoleIds { get; set; } = new List<int>();
    }

    public class SearchRoleRequest
    {
        public string? Search { get; set; }
        public bool? IsActive { get; set; }
    }
}
