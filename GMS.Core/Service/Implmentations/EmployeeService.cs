using AutoMapper;
using Domin.Contract;
using Domin.GymEntities;
using Services.Abstraction.Contract;
using Shared.DTOs.EmployeeDTOs;
using Shared.Extensions;

namespace Services.Implmentations {
	public class EmployeeService(IUnitOfWork unitOfWork, IMapper mapper) : IEmployeeService {

		public async Task<IEnumerable<EmployeeDTO>> GetAllEmployees(string? search = null, int? jobTitle = null, string? status = null) {
			var employees = await unitOfWork.GetRepository<Employee>().GetAllAsync();
			if (employees is null || !employees.Any()) return [];

			IEnumerable<EmployeeDTO> result = mapper.Map<IEnumerable<EmployeeDTO>>(employees);

			if (!string.IsNullOrWhiteSpace(search)) {
				var term = search.Trim();
				result = result.Where(E =>
					E.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
					E.Email.Contains(term, StringComparison.OrdinalIgnoreCase) ||
					E.Phone.Contains(term, StringComparison.OrdinalIgnoreCase) ||
					E.JobTitle.Contains(term, StringComparison.OrdinalIgnoreCase));
			}

			if (jobTitle is > 0) {
				// The DTO Carries The Display Label, So Match On That Rather Than The Member Name.
				var wanted = ((Domin.Enums.JobTitle)jobTitle.Value).GetDisplayName();
				result = result.Where(E => E.JobTitle == wanted);
			}

			result = status switch {
				"active" => result.Where(E => E.IsActive),
				"former" => result.Where(E => !E.IsActive),
				_ => result,
			};

			// Current Staff First, Then Alphabetically — A Former Employee Is History, Not News.
			return [.. result.OrderByDescending(E => E.IsActive).ThenBy(E => E.Name)];
		}

		public async Task<EmployeeDTO?> GetEmployeeDetails(int employeeId) {
			var employee = await unitOfWork.GetRepository<Employee>().GetAsync(employeeId);
			return employee is null ? null : mapper.Map<EmployeeDTO>(employee);
		}

		public async Task<bool> CreateEmployee(CreateEmployeeDTO createdEmployee) {
			try {
				if (await IsEmailExist(createdEmployee.Email) || await IsPhoneExist(createdEmployee.Phone)) return false;

				var employee = mapper.Map<Employee>(createdEmployee);
				employee.IsActive = true;
				employee.CreatedAt = DateOnly.FromDateTime(DateTime.Now);
				employee.UpdatedAt = DateOnly.FromDateTime(DateTime.Now);

				await unitOfWork.GetRepository<Employee>().AddAsync(employee);
				return await unitOfWork.SaveChangesAsync() > 0;
			} catch (Exception) {
				return false;
			}
		}

		public async Task<EmployeeToUpdateDTO?> GetEmployeeToUpdate(int employeeId) {
			var employee = await unitOfWork.GetRepository<Employee>().GetAsync(employeeId);
			return employee is null ? null : mapper.Map<EmployeeToUpdateDTO>(employee);
		}

		public async Task<bool> UpdateEmployeeDetails(EmployeeToUpdateDTO updatedEmployee, int employeeId) {
			try {
				var repo = unitOfWork.GetRepository<Employee>();
				var employeeToUpdate = await repo.GetAsync(employeeId);

				if (employeeToUpdate is null
					|| await IsEmailExist(employeeId, updatedEmployee.Email)
					|| await IsPhoneExist(employeeId, updatedEmployee.Phone)) return false;

				employeeToUpdate.Email = updatedEmployee.Email;
				employeeToUpdate.Phone = updatedEmployee.Phone;
				employeeToUpdate.Photo = updatedEmployee.Photo;
				employeeToUpdate.Address.BuildingNumber = updatedEmployee.BuildingNumber;
				employeeToUpdate.Address.Street = updatedEmployee.Street;
				employeeToUpdate.Address.City = updatedEmployee.City;
				employeeToUpdate.JobTitle = updatedEmployee.JobTitle;
				employeeToUpdate.EmploymentType = updatedEmployee.EmploymentType;
				employeeToUpdate.MonthlySalary = updatedEmployee.MonthlySalary;
				employeeToUpdate.IsActive = updatedEmployee.IsActive;
				employeeToUpdate.UpdatedAt = DateOnly.FromDateTime(DateTime.Now);

				repo.Update(employeeToUpdate);
				return await unitOfWork.SaveChangesAsync() > 0;
			} catch (Exception) {
				return false;
			}
		}

		public async Task<bool> RemoveEmployee(int employeeId) {
			try {
				var repo = unitOfWork.GetRepository<Employee>();
				var employeeToRemove = await repo.GetAsync(employeeId);
				if (employeeToRemove is null) return false;

				repo.Delete(employeeToRemove);
				return await unitOfWork.SaveChangesAsync() > 0;
			} catch (Exception) {
				return false;
			}
		}

		public async Task<string?> GetEmployeePhoto(int employeeId)
			=> (await unitOfWork.GetRepository<Employee>().GetAsync(employeeId))?.Photo;

		public async Task<decimal> GetMonthlyPayroll() {
			var employees = await unitOfWork.GetRepository<Employee>().GetAllAsync(E => E.IsActive);
			return employees.Sum(E => E.MonthlySalary);
		}

		#region Helper Methods
		private async Task<bool> IsEmailExist(string email)
			=> (await unitOfWork.GetRepository<Employee>().GetAllAsync(E => E.Email == email)).Any();

		private async Task<bool> IsEmailExist(int employeeId, string email)
			=> (await unitOfWork.GetRepository<Employee>().GetAllAsync(E => E.Email == email && E.Id != employeeId)).Any();

		private async Task<bool> IsPhoneExist(string phone)
			=> (await unitOfWork.GetRepository<Employee>().GetAllAsync(E => E.Phone == phone)).Any();

		private async Task<bool> IsPhoneExist(int employeeId, string phone)
			=> (await unitOfWork.GetRepository<Employee>().GetAllAsync(E => E.Phone == phone && E.Id != employeeId)).Any();
		#endregion
	}
}
