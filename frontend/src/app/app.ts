import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import {
  Employee,
  EmployeeRequest,
  SchedulerService,
  Shift,
  ShiftRequest,
} from './scheduler.service';
import { EmployeeManagementComponent } from './components/employee-management/employee-management.component';
import {
  ScheduleCalendarComponent,
  ScheduleDay,
  ScheduleView,
  startOfWeek,
  dateKey,
  timeInputValue,
} from './components/schedule-calendar/schedule-calendar.component';
import { ShiftAssignmentComponent } from './components/shift-assignment/shift-assignment.component';

@Component({
  imports: [ScheduleCalendarComponent, EmployeeManagementComponent, ShiftAssignmentComponent],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App implements OnInit {
  private readonly schedulerService = inject(SchedulerService);

  protected readonly employees = signal<Employee[]>([]);
  protected readonly shifts = signal<Shift[]>([]);
  protected readonly viewDate = signal(new Date());
  protected readonly scheduleView = signal<ScheduleView>('week');
  protected readonly loading = signal(true);
  protected readonly savingEmployee = signal(false);
  protected readonly savingShift = signal(false);
  protected readonly busyShiftId = signal<number | null>(null);
  protected readonly busyEmployeeId = signal<number | null>(null);
  protected readonly errorMessage = signal('');
  protected readonly successMessage = signal('');
  protected readonly editingEmployeeId = signal<number | null>(null);
  protected readonly editingShiftId = signal<number | null>(null);
  protected readonly weekDays = computed<ScheduleDay[]>(() =>
    Array.from({ length: 7 }, (_, index) => {
      const date = startOfWeek(this.viewDate());
      date.setDate(date.getDate() + index);
      return { date, key: dateKey(date) };
    }),
  );
  protected readonly monthDays = computed<ScheduleDay[]>(() => {
    const date = this.viewDate();
    const firstOfMonth = new Date(date.getFullYear(), date.getMonth(), 1);
    const firstVisibleDate = startOfWeek(firstOfMonth);
    const lastOfMonth = new Date(date.getFullYear(), date.getMonth() + 1, 0);
    const lastVisibleDate = new Date(lastOfMonth);
    lastVisibleDate.setDate(lastVisibleDate.getDate() + 6 - ((lastVisibleDate.getDay() + 6) % 7));
    const dayCount = (lastVisibleDate.getTime() - firstVisibleDate.getTime()) / 86_400_000 + 1;

    return Array.from({ length: dayCount }, (_, index) => {
      const day = new Date(firstVisibleDate);
      day.setDate(day.getDate() + index);
      return { date: day, key: dateKey(day) };
    });
  });
  protected readonly displayedDays = computed(() =>
    this.scheduleView() === 'week' ? this.weekDays() : this.monthDays(),
  );
  protected readonly scheduleLabel = computed(() => {
    if (this.scheduleView() === 'month') {
      return new Intl.DateTimeFormat(undefined, { month: 'long', year: 'numeric' }).format(
        this.viewDate(),
      );
    }
    const days = this.weekDays();
    const first = days[0].date;
    const last = days[6].date;
    const firstLabel = new Intl.DateTimeFormat(undefined, {
      month: 'short',
      day: 'numeric',
    }).format(first);
    const lastLabel = new Intl.DateTimeFormat(undefined, {
      month: first.getMonth() === last.getMonth() ? undefined : 'short',
      day: 'numeric',
      year: first.getFullYear() === last.getFullYear() ? undefined : 'numeric',
    }).format(last);
    return `${firstLabel} – ${lastLabel}, ${last.getFullYear()}`;
  });

  protected employeeForm: EmployeeRequest = emptyEmployee();
  protected shiftForm: ShiftRequest = {
    employeeId: 0,
    date: dateKey(new Date()),
    startTime: '09:00',
    endTime: '17:00',
  };

  ngOnInit(): void {
    this.loadEmployees();
    this.loadShifts();
  }

  protected moveWeek(offset: number): void {
    const date = new Date(this.viewDate());
    if (this.scheduleView() === 'week') {
      date.setDate(date.getDate() + offset * 7);
    } else {
      date.setDate(1);
      date.setMonth(date.getMonth() + offset);
    }
    this.viewDate.set(date);
    this.loadShifts();
  }

  protected setScheduleView(view: ScheduleView): void {
    if (view === this.scheduleView()) {
      return;
    }
    this.scheduleView.set(view);
    this.loadShifts();
  }

  protected goToToday(): void {
    this.viewDate.set(new Date());
    this.loadShifts();
  }

  protected startEmployeeEdit(employee: Employee): void {
    this.editingEmployeeId.set(employee.id);
    this.employeeForm = {
      firstName: employee.firstName,
      lastName: employee.lastName,
      jobTitle: employee.jobTitle ?? '',
      email: employee.email ?? '',
      contactPhoneNumber: employee.contactPhoneNumber ?? '',
    };
    this.errorMessage.set('');
    this.successMessage.set('');
  }

  protected cancelEmployeeEdit(): void {
    this.editingEmployeeId.set(null);
    this.employeeForm = emptyEmployee();
  }

  protected saveEmployee(): void {
    const request: EmployeeRequest = {
      ...this.employeeForm,
      firstName: this.employeeForm.firstName.trim(),
      lastName: this.employeeForm.lastName.trim(),
      jobTitle: this.employeeForm.jobTitle.trim(),
      email: this.employeeForm.email.trim(),
      contactPhoneNumber: this.employeeForm.contactPhoneNumber.trim(),
    };
    if (!request.firstName || !request.lastName || this.savingEmployee()) {
      return;
    }

    this.savingEmployee.set(true);
    this.clearNotifications();
    const employeeId = this.editingEmployeeId();
    const operation =
      employeeId === null
        ? this.schedulerService.createEmployee(request)
        : this.schedulerService.updateEmployee(employeeId, request);
    operation.subscribe({
      next: (employee) => {
        this.employees.update((employees) => {
          const updated =
            employeeId === null
              ? [...employees, employee]
              : employees.map((item) => (item.id === employee.id ? employee : item));
          return updated.sort(
            (a, b) =>
              a.lastName.localeCompare(b.lastName) || a.firstName.localeCompare(b.firstName),
          );
        });
        if (employeeId === null && !this.shiftForm.employeeId) {
          this.shiftForm = { ...this.shiftForm, employeeId: employee.id };
        }
        this.cancelEmployeeEdit();
        this.successMessage.set(employeeId === null ? 'Employee added.' : 'Employee updated.');
        this.savingEmployee.set(false);
      },
      error: (error: HttpErrorResponse) => {
        this.errorMessage.set(this.describeError(error, 'Unable to save this employee.'));
        this.savingEmployee.set(false);
      },
    });
  }

  protected deleteEmployee(employee: Employee): void {
    if (this.busyEmployeeId() !== null) {
      return;
    }

    this.busyEmployeeId.set(employee.id);
    this.clearNotifications();
    this.schedulerService.deleteEmployee(employee.id).subscribe({
      next: () => {
        this.employees.update((employees) => employees.filter((item) => item.id !== employee.id));
        if (this.shiftForm.employeeId === employee.id) {
          this.shiftForm = {
            ...this.shiftForm,
            employeeId: this.employees()[0]?.id ?? 0,
          };
        }
        if (this.editingEmployeeId() === employee.id) {
          this.cancelEmployeeEdit();
        }
        this.successMessage.set('Employee removed.');
        this.busyEmployeeId.set(null);
      },
      error: (error: HttpErrorResponse) => {
        this.errorMessage.set(this.describeError(error, 'Unable to remove this employee.'));
        this.busyEmployeeId.set(null);
      },
    });
  }

  protected editShift(shift: Shift): void {
    this.editingShiftId.set(shift.id);
    this.shiftForm = {
      employeeId: shift.employeeId,
      date: shift.date,
      startTime: timeInputValue(shift.startTime),
      endTime: timeInputValue(shift.endTime),
    };
    this.clearNotifications();
  }

  protected newShiftFor(date: string): void {
    this.editingShiftId.set(null);
    this.shiftForm = {
      employeeId: this.shiftForm.employeeId || this.employees()[0]?.id || 0,
      date,
      startTime: '09:00',
      endTime: '17:00',
    };
    this.clearNotifications();
  }

  protected cancelShiftEdit(): void {
    this.editingShiftId.set(null);
    this.newShiftFor(this.weekDays()[0].key);
  }

  protected saveShift(): void {
    if (
      !this.shiftForm.employeeId ||
      !this.shiftForm.date ||
      !this.shiftForm.startTime ||
      !this.shiftForm.endTime ||
      this.shiftForm.endTime <= this.shiftForm.startTime ||
      this.savingShift()
    ) {
      return;
    }

    this.savingShift.set(true);
    this.clearNotifications();
    const shiftId = this.editingShiftId();
    const operation =
      shiftId === null
        ? this.schedulerService.createShift(this.shiftForm)
        : this.schedulerService.updateShift(shiftId, this.shiftForm);
    operation.subscribe({
      next: (shift) => {
        const isVisible = this.displayedDays().some((day) => day.key === shift.date);
        if (!isVisible) {
          this.viewDate.set(new Date(`${shift.date}T00:00:00`));
          this.loadShifts();
        } else {
          this.shifts.update((shifts) =>
            shiftId === null
              ? [...shifts, shift]
              : shifts.map((item) => (item.id === shift.id ? shift : item)),
          );
        }
        this.editingShiftId.set(null);
        this.successMessage.set(shiftId === null ? 'Shift scheduled.' : 'Shift updated.');
        this.savingShift.set(false);
      },
      error: (error: HttpErrorResponse) => {
        this.errorMessage.set(this.describeError(error, 'Unable to save this shift.'));
        this.savingShift.set(false);
      },
    });
  }

  protected deleteShift(shift: Shift): void {
    if (this.busyShiftId() !== null) {
      return;
    }

    this.busyShiftId.set(shift.id);
    this.clearNotifications();
    this.schedulerService.deleteShift(shift.id).subscribe({
      next: () => {
        this.shifts.update((shifts) => shifts.filter((item) => item.id !== shift.id));
        this.successMessage.set('Shift removed.');
        this.busyShiftId.set(null);
      },
      error: (error: HttpErrorResponse) => {
        this.errorMessage.set(this.describeError(error, 'Unable to remove this shift.'));
        this.busyShiftId.set(null);
      },
    });
  }

  private loadEmployees(): void {
    this.schedulerService.getEmployees().subscribe({
      next: (employees) => {
        this.employees.set(employees);
        if (!this.shiftForm.employeeId && employees.length > 0) {
          this.shiftForm = { ...this.shiftForm, employeeId: employees[0].id };
        }
      },
      error: (error: HttpErrorResponse) => {
        this.errorMessage.set(this.describeError(error, 'Unable to load employees.'));
      },
    });
  }

  private loadShifts(): void {
    const days = this.displayedDays();
    const start = days[0].key;
    const end = days[days.length - 1].key;
    this.loading.set(true);
    this.schedulerService.getShifts(start, end).subscribe({
      next: (shifts) => {
        this.shifts.set(shifts);
        this.loading.set(false);
      },
      error: (error: HttpErrorResponse) => {
        this.errorMessage.set(this.describeError(error, 'Unable to load the schedule.'));
        this.loading.set(false);
      },
    });
  }

  private clearNotifications(): void {
    this.errorMessage.set('');
    this.successMessage.set('');
  }

  private describeError(error: HttpErrorResponse, fallback: string): string {
    const message = error.error?.error;
    return typeof message === 'string' ? message : fallback;
  }
}

function emptyEmployee(): EmployeeRequest {
  return { firstName: '', lastName: '', jobTitle: '', email: '', contactPhoneNumber: '' };
}

