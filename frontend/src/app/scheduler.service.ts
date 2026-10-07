import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

export interface Employee {
  id: number;
  firstName: string;
  lastName: string;
  jobTitle: string | null;
  email: string | null;
  contactPhoneNumber: string | null;
}

export interface EmployeeRequest {
  firstName: string;
  lastName: string;
  jobTitle: string;
  email: string;
  contactPhoneNumber: string;
}

export interface Shift {
  id: number;
  employeeId: number;
  date: string;
  startTime: string;
  endTime: string;
}

export interface ShiftRequest {
  employeeId: number;
  date: string;
  startTime: string;
  endTime: string;
}

@Injectable({ providedIn: 'root' })
export class SchedulerService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5000/api';

  getEmployees(): Observable<Employee[]> {
    return this.http.get<Employee[]>(`${this.apiUrl}/employees`);
  }

  createEmployee(employee: EmployeeRequest): Observable<Employee> {
    return this.http.post<Employee>(`${this.apiUrl}/employees`, employee);
  }

  updateEmployee(id: number, employee: EmployeeRequest): Observable<Employee> {
    return this.http.put<Employee>(`${this.apiUrl}/employees/${id}`, employee);
  }

  deleteEmployee(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/employees/${id}`);
  }

  getShifts(startDate: string, endDate: string): Observable<Shift[]> {
    const params = new HttpParams().set('startDate', startDate).set('endDate', endDate);
    return this.http.get<Shift[]>(`${this.apiUrl}/shifts`, { params });
  }

  createShift(shift: ShiftRequest): Observable<Shift> {
    return this.http.post<Shift>(`${this.apiUrl}/shifts`, shift);
  }

  updateShift(id: number, shift: ShiftRequest): Observable<Shift> {
    return this.http.put<Shift>(`${this.apiUrl}/shifts/${id}`, shift);
  }

  deleteShift(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/shifts/${id}`);
  }
}
