import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { App } from './app';
import {
  Employee,
  EmployeeRequest,
  SchedulerService,
  Shift,
  ShiftRequest,
} from './scheduler.service';

class SchedulerServiceStub {
  getEmployees = vi.fn<() => Observable<Employee[]>>();
  createEmployee = vi.fn<(employee: EmployeeRequest) => Observable<Employee>>();
  updateEmployee = vi.fn<(id: number, employee: EmployeeRequest) => Observable<Employee>>();
  deleteEmployee = vi.fn<(id: number) => Observable<void>>();
  getShifts = vi.fn<(startDate: string, endDate: string) => Observable<Shift[]>>();
  createShift = vi.fn<(shift: ShiftRequest) => Observable<Shift>>();
  updateShift = vi.fn<(id: number, shift: ShiftRequest) => Observable<Shift>>();
  deleteShift = vi.fn<(id: number) => Observable<void>>();
}

const employees: Employee[] = [
  {
    id: 1,
    firstName: 'Ada',
    lastName: 'Lovelace',
    jobTitle: 'Barista',
    email: 'ada@example.test',
    contactPhoneNumber: null,
  },
];

const shift = (date: string): Shift => ({
  id: 10,
  employeeId: 1,
  date,
  startTime: '09:00:00',
  endTime: '17:00:00',
});

describe('App', () => {
  let fixture: ComponentFixture<App>;
  let schedulerService: SchedulerServiceStub;

  beforeEach(async () => {
    schedulerService = new SchedulerServiceStub();
    schedulerService.getEmployees.mockReturnValue(of(employees));
    schedulerService.createEmployee.mockImplementation((request) =>
      of({ ...employees[0], ...request, id: 2 }),
    );
    schedulerService.updateEmployee.mockImplementation((id, request) =>
      of({ ...employees[0], ...request, id }),
    );
    schedulerService.deleteEmployee.mockReturnValue(of(void 0));
    schedulerService.getShifts.mockReturnValue(of([]));
    schedulerService.createShift.mockImplementation((request) => of({ ...request, id: 10 }));
    schedulerService.updateShift.mockImplementation((id, request) => of({ ...request, id }));
    schedulerService.deleteShift.mockReturnValue(of(void 0));

    await TestBed.configureTestingModule({
      imports: [App],
      providers: [{ provide: SchedulerService, useValue: schedulerService }],
    }).compileComponents();

    fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  });

  it("loads team members and the current week's shifts", () => {
    expect(schedulerService.getEmployees).toHaveBeenCalledOnce();
    expect(schedulerService.getShifts).toHaveBeenCalledOnce();
    const [startDate, endDate] = schedulerService.getShifts.mock.calls[0];
    expect(new Date(`${startDate}T00:00:00`).getDay()).toBe(1);
    expect((Date.parse(endDate) - Date.parse(startDate)) / 86_400_000).toBe(6);
    expect(fixture.nativeElement.textContent).toContain('Weekly schedule');
    expect(fixture.nativeElement.textContent).toContain('Ada Lovelace');
  });

  it('displays shifts in the weekly calendar', async () => {
    const requestedDate = schedulerService.getShifts.mock.calls[0][0];
    schedulerService.getShifts.mockReturnValue(of([shift(requestedDate)]));
    fixture.destroy();

    fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('09:00–17:00');
    expect(fixture.nativeElement.textContent).toContain('Ada Lovelace');
  });

  it('creates a shift for the selected employee and date', async () => {
    const dateInput = fixture.nativeElement.querySelector(
      'input[name="date"]',
    ) as HTMLInputElement;
    dateInput.value = '2026-10-08';
    dateInput.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    fixture.nativeElement.querySelector('.shift-editor form').dispatchEvent(new Event('submit'));
    await fixture.whenStable();

    expect(schedulerService.createShift).toHaveBeenCalledWith({
      employeeId: 1,
      date: '2026-10-08',
      startTime: '09:00',
      endTime: '17:00',
    });
  });

  it('adds the first employee and makes them available for shift assignment', async () => {
    schedulerService.getEmployees.mockReturnValue(of([]));
    fixture.destroy();
    fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const firstName = fixture.nativeElement.querySelector(
      '.management-panel input[name="firstName"]',
    ) as HTMLInputElement;
    const lastName = fixture.nativeElement.querySelector(
      '.management-panel input[name="lastName"]',
    ) as HTMLInputElement;
    firstName.value = 'Grace';
    firstName.dispatchEvent(new Event('input'));
    lastName.value = 'Hopper';
    lastName.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    fixture.nativeElement.querySelector('.management-panel form').dispatchEvent(
      new Event('submit'),
    );
    await fixture.whenStable();
    fixture.detectChanges();

    expect(schedulerService.createEmployee).toHaveBeenCalledWith(
      expect.objectContaining({ firstName: 'Grace', lastName: 'Hopper' }),
    );
    expect(fixture.nativeElement.textContent).toContain('Grace Hopper');
    const employeeSelect = fixture.nativeElement.querySelector(
      '.shift-editor select',
    ) as HTMLSelectElement;
    expect(employeeSelect.selectedOptions[0].textContent).toBe('Grace Hopper');
  });

  it('prevents submitting a shift when the end is not later than the start', () => {
    const startInput = fixture.nativeElement.querySelector(
      'input[name="startTime"]',
    ) as HTMLInputElement;
    const endInput = fixture.nativeElement.querySelector(
      'input[name="endTime"]',
    ) as HTMLInputElement;
    startInput.value = '15:00';
    startInput.dispatchEvent(new Event('input'));
    endInput.value = '14:00';
    endInput.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect(
      (fixture.nativeElement.querySelector('.shift-editor button[type="submit"]') as HTMLButtonElement)
        .disabled,
    ).toBe(true);
    expect(schedulerService.createShift).not.toHaveBeenCalled();
  });

  it('shows the API conflict when a shift overlaps another shift', async () => {
    schedulerService.createShift.mockReturnValue(
      throwError(
        () => new HttpErrorResponse({ error: { error: 'This employee already has a shift.' } }),
      ),
    );
    fixture.nativeElement.querySelector('.shift-editor form').dispatchEvent(new Event('submit'));
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[role="alert"]').textContent).toContain(
      'already has a shift',
    );
  });

  it('updates employee details', async () => {
    (fixture.nativeElement.querySelector('.employee-actions button') as HTMLButtonElement).click();
    fixture.detectChanges();
    const firstName = fixture.nativeElement.querySelector(
      '.management-panel input[name="firstName"]',
    ) as HTMLInputElement;
    firstName.value = 'Augusta';
    firstName.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    fixture.nativeElement.querySelector('.data-form').dispatchEvent(new Event('submit'));
    await fixture.whenStable();
    fixture.detectChanges();

    expect(schedulerService.updateEmployee).toHaveBeenCalledWith(
      1,
      expect.objectContaining({ firstName: 'Augusta', lastName: 'Lovelace' }),
    );
    expect(fixture.nativeElement.textContent).toContain('Augusta Lovelace');
  });

  it('loads a different week when navigating', async () => {
    const previousStart = schedulerService.getShifts.mock.calls[0][0];
    (fixture.nativeElement.querySelector('[aria-label="Next week"]') as HTMLButtonElement).click();
    await fixture.whenStable();

    expect(schedulerService.getShifts).toHaveBeenCalledTimes(2);
    expect(schedulerService.getShifts.mock.calls[1][0]).not.toBe(previousStart);
  });
});
