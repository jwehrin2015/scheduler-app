import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Employee, ShiftRequest } from '../../scheduler.service';

@Component({
  imports: [FormsModule],
  selector: 'app-shift-assignment',
  styleUrl: './shift-assignment.component.css',
  templateUrl: './shift-assignment.component.html',
})
export class ShiftAssignmentComponent {
  @Input() employees: Employee[] = [];
  @Input() shiftForm: ShiftRequest = {
    employeeId: 0,
    date: '',
    startTime: '09:00',
    endTime: '17:00',
  };
  @Input() editingShiftId: number | null = null;
  @Input() savingShift = false;

  @Output() save = new EventEmitter<void>();
  @Output() cancelEdit = new EventEmitter<void>();

  employeeName(employee: Employee): string {
    return [employee.firstName, employee.lastName].filter(Boolean).join(' ');
  }
}
