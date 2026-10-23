using System;
using SchedulerApp.Api.Models;

namespace SchedulerApp.Api.Services.EmployeeService;

public interface IEmployeeService
{
    Task<List<Employee>> GetAllEmployeesAsync();
    Task<IResult> CreateEmployeeAsync(EmployeeRequest request);
    Task<IResult> UpdateEmployeeAsync(int id, EmployeeRequest request);
    Task<IResult> DeleteEmployeeAsync(int id);
}
