import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { EmployeeRequest, SchedulerService, ShiftRequest } from './scheduler.service';

describe('SchedulerService', () => {
  let service: SchedulerService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(SchedulerService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  it('gets employee records', () => {
    service.getEmployees().subscribe((employees) => expect(employees).toEqual([]));
    const request = httpTesting.expectOne('http://localhost:5000/api/employees');
    expect(request.request.method).toBe('GET');
    request.flush([]);
  });

  it('creates and updates employees', () => {
    const employee: EmployeeRequest = {
      firstName: 'Ada',
      lastName: 'Lovelace',
      jobTitle: 'Barista',
      email: '',
      contactPhoneNumber: '',
    };

    service.createEmployee(employee).subscribe();
    const createRequest = httpTesting.expectOne('http://localhost:5000/api/employees');
    expect(createRequest.request.method).toBe('POST');
    expect(createRequest.request.body).toEqual(employee);
    createRequest.flush({ id: 3, ...employee });

    service.updateEmployee(3, employee).subscribe();
    const updateRequest = httpTesting.expectOne('http://localhost:5000/api/employees/3');
    expect(updateRequest.request.method).toBe('PUT');
    expect(updateRequest.request.body).toEqual(employee);
    updateRequest.flush({ id: 3, ...employee });
  });

  it('deletes an employee by ID', () => {
    service.deleteEmployee(3).subscribe();
    const request = httpTesting.expectOne('http://localhost:5000/api/employees/3');
    expect(request.request.method).toBe('DELETE');
    request.flush(null);
  });

  it('queries shifts by date range', () => {
    service.getShifts('2026-10-05', '2026-10-11').subscribe((shifts) =>
      expect(shifts).toEqual([]),
    );
    const request = httpTesting.expectOne(
      (candidate) =>
        candidate.url === 'http://localhost:5000/api/shifts' &&
        candidate.params.get('startDate') === '2026-10-05' &&
        candidate.params.get('endDate') === '2026-10-11',
    );
    expect(request.request.method).toBe('GET');
    request.flush([]);
  });

  it('creates, updates, and deletes shifts', () => {
    const shift: ShiftRequest = {
      employeeId: 3,
      date: '2026-10-08',
      startTime: '09:00',
      endTime: '17:00',
    };

    service.createShift(shift).subscribe();
    const createRequest = httpTesting.expectOne('http://localhost:5000/api/shifts');
    expect(createRequest.request.method).toBe('POST');
    expect(createRequest.request.body).toEqual(shift);
    createRequest.flush({ id: 4, ...shift });

    service.updateShift(4, shift).subscribe();
    const updateRequest = httpTesting.expectOne('http://localhost:5000/api/shifts/4');
    expect(updateRequest.request.method).toBe('PUT');
    expect(updateRequest.request.body).toEqual(shift);
    updateRequest.flush({ id: 4, ...shift });

    service.deleteShift(4).subscribe();
    const deleteRequest = httpTesting.expectOne('http://localhost:5000/api/shifts/4');
    expect(deleteRequest.request.method).toBe('DELETE');
    deleteRequest.flush(null);
  });
});
