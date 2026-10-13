import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Employee, EmployeeRequest } from '../../scheduler.service';

@Component({
  imports: [FormsModule],
  selector: 'app-employee-management',
  styleUrl: './employee-management.component.css',
  templateUrl: './employee-management.component.html',
})
export class EmployeeManagementComponent {
  @Input() employees: Employee[] = [];
  @Input() employeeForm: EmployeeRequest = {
    firstName: '',
    lastName: '',
    jobTitle: '',
    email: '',
    contactPhoneNumber: '',
  };
  @Input() editingEmployeeId: number | null = null;
  @Input() savingEmployee = false;
  @Input() busyEmployeeId: number | null = null;

  @Output() save = new EventEmitter<void>();
  @Output() cancelEdit = new EventEmitter<void>();
  @Output() startEdit = new EventEmitter<Employee>();
  @Output() remove = new EventEmitter<Employee>();

  employeeName(employee: Employee): string {
    return [employee.firstName, employee.lastName].filter(Boolean).join(' ');
  }
}
