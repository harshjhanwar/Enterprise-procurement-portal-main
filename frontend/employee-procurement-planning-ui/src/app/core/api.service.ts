import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Dashboard, LoginResponse, Paged, Plan, User } from './models';

declare global {
  interface Window {
    __EPP_CONFIG__?: {
      apiBaseUrl?: string;
    };
  }
}

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = (window.__EPP_CONFIG__?.apiBaseUrl ?? '').replace(/\/$/, '');

  login(email: string, password: string) {
    return this.http.post<LoginResponse>(`${this.baseUrl}/api/v1/auth/login`, { email, password });
  }

  me() {
    return this.http.get<{ id: string; email: string; displayName: string; roles: string[] }>(
      `${this.baseUrl}/api/v1/auth/me`
    );
  }

  dashboard() {
    return this.http.get<Dashboard>(`${this.baseUrl}/api/v1/plans/dashboard`);
  }

  plans(query: Record<string, unknown> = {}) {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(query)) {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, String(value));
      }
    }
    return this.http.get<Paged<Plan>>(`${this.baseUrl}/api/v1/plans`, { params });
  }

  plan(id: string) {
    return this.http.get<Plan>(`${this.baseUrl}/api/v1/plans/${id}`);
  }

  create(body: unknown) {
    return this.http.post<Plan>(`${this.baseUrl}/api/v1/plans`, body);
  }

  update(id: string, body: unknown) {
    return this.http.put<Plan>(`${this.baseUrl}/api/v1/plans/${id}`, body);
  }

  transition(id: string, action: string, rowVersion: string, comment?: string) {
    return this.http.post<Plan>(`${this.baseUrl}/api/v1/plans/${id}/${action}`, {
      rowVersion,
      comment
    });
  }

  departments() {
    return this.http.get<Array<{ id: string; name: string; code: string }>>(
      `${this.baseUrl}/api/v1/departments`
    );
  }

  users() {
    return this.http.get<User[]>(`${this.baseUrl}/api/v1/admin/users`);
  }

  updateUser(id: string, body: { roles: string[]; isActive: boolean }) {
    return this.http.put<void>(`${this.baseUrl}/api/v1/admin/users/${id}`, body);
  }
}
