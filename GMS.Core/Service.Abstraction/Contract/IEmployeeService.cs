using Shared.DTOs.EmployeeDTOs;

namespace Services.Abstraction.Contract {
    public interface IEmployeeService {
        Task<IEnumerable<EmployeeDTO>> GetAllEmployees(string? search = null, int? jobTitle = null, string? status = null);
        Task<EmployeeDTO?> GetEmployeeDetails(int employeeId);
        Task<bool> CreateEmployee(CreateEmployeeDTO createdEmployee);
        Task<EmployeeToUpdateDTO?> GetEmployeeToUpdate(int employeeId);
        Task<bool> UpdateEmployeeDetails(EmployeeToUpdateDTO updatedEmployee, int employeeId);
        Task<bool> RemoveEmployee(int employeeId);
        Task<string?> GetEmployeePhoto(int employeeId);

        /// <summary>Total Gross Pay Owed Each Month, Counting Only Staff Still Employed.</summary>
        Task<decimal> GetMonthlyPayroll();
    }
}
