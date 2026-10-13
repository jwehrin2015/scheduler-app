import { DatePipe } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { Employee, Shift } from '../../scheduler.service';

export interface ScheduleDay {
  date: Date;
  key: string;
}

export type ScheduleView = 'week' | 'month';

@Component({
  imports: [DatePipe],
  selector: 'app-schedule-calendar',
  styleUrl: './schedule-calendar.component.css',
  templateUrl: './schedule-calendar.component.html',
})
export class ScheduleCalendarComponent {
  @Input() days: ScheduleDay[] = [];
  @Input() employees: Employee[] = [];
  @Input() shifts: Shift[] = [];
  @Input() loading = false;
  @Input() busyShiftId: number | null = null;
  @Input() scheduleLabel = '';
  @Input() scheduleView: ScheduleView = 'week';
  @Input() viewDate = new Date();

  @Output() viewChange = new EventEmitter<ScheduleView>();
  @Output() navigate = new EventEmitter<number>();
  @Output() addShift = new EventEmitter<string>();
  @Output() editShift = new EventEmitter<Shift>();
  @Output() removeShift = new EventEmitter<Shift>();

  shiftsForDay(date: string): Shift[] {
    return this.shifts
      .filter((shift) => shift.date === date)
      .sort((a, b) => a.startTime.localeCompare(b.startTime));
  }

  employeeFor(id: number): Employee | undefined {
    return this.employees.find((employee) => employee.id === id);
  }

  employeeName(employee: Employee | undefined): string {
    return employee
      ? [employee.firstName, employee.lastName].filter(Boolean).join(' ')
      : 'Unknown employee';
  }

  displayTime(time: string): string {
    return time.slice(0, 5);
  }

  isOutsideCurrentMonth(date: Date): boolean {
    return (
      date.getMonth() !== this.viewDate.getMonth() ||
      date.getFullYear() !== this.viewDate.getFullYear()
    );
  }
}

export function startOfWeek(date: Date): Date {
  const result = new Date(date.getFullYear(), date.getMonth(), date.getDate());
  const daysSinceMonday = (result.getDay() + 6) % 7;
  result.setDate(result.getDate() - daysSinceMonday);
  return result;
}

export function dateKey(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

export function timeInputValue(time: string): string {
  return time.slice(0, 5);
}
