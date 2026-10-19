import { computed, Injectable, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { ApiService } from './api.service';
import { LoginResponse, Role } from './models';

interface StoredUser {
  displayName: string;
  roles: Role[];
  userId: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);
  private readonly token = signal<string | null>(localStorage.getItem('epp_token'));

  readonly isAuthenticated = computed(() => !!this.token());

  login(email: string, password: string): Observable<LoginResponse> {
    return this.api.login(email, password).pipe(
      tap(response => {
        const user: StoredUser = {
          displayName: response.displayName,
          roles: response.roles,
          userId: this.extractUserId(response.accessToken)
        };

        localStorage.setItem('epp_token', response.accessToken);
        localStorage.setItem('epp_user', JSON.stringify(user));
        localStorage.setItem('epp_token_expires_at', response.expiresAtUtc);
        this.token.set(response.accessToken);
      })
    );
  }

  logout(): void {
    localStorage.removeItem('epp_token');
    localStorage.removeItem('epp_user');
    localStorage.removeItem('epp_token_expires_at');
    this.token.set(null);
    void this.router.navigate(['/login']);
  }

  user(): StoredUser {
    try {
      return JSON.parse(localStorage.getItem('epp_user') ?? '{}') as StoredUser;
    } catch {
      return { displayName: '', roles: [], userId: '' };
    }
  }

  userId(): string {
    return this.user().userId ?? '';
  }

  hasRole(role: Role): boolean {
    return this.user().roles?.includes(role) ?? false;
  }

  handleUnauthorized(): void {
    this.logout();
  }

  private extractUserId(token: string): string {
    try {
      const payload = token.split('.')[1];
      const normalized = payload.replace(/-/g, '+').replace(/_/g, '/');
      const padded = normalized.padEnd(Math.ceil(normalized.length / 4) * 4, '=');
      const decoded = JSON.parse(atob(padded)) as Record<string, unknown>;
      return String(decoded['sub'] ?? decoded['nameid'] ?? decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ?? '');
    } catch {
      return '';
    }
  }
}
