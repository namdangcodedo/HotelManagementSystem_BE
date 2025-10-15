namespace AppBackend.Services.ApiModels.EmployeeModel
{
    public class BanEmployeeRequest
    {
        public int EmployeeId { get; set; }
        public bool IsLocked { get; set; }
    }
}

