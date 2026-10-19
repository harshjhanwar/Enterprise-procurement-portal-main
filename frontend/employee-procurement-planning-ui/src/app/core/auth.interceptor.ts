import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';

declare global {
  interface Window {
    __EPP_CONFIG__?: { apiBaseUrl?: string };
  }
}

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const token = localStorage.getItem('epp_token');
  const apiBaseUrl = (window.__EPP_CONFIG__?.apiBaseUrl ?? '').replace(/\/$/, '');
  const isApiRequest = request.url.startsWith(`${apiBaseUrl}/api/`) || request.url.startsWith('/api/');

  const authenticatedRequest = token && isApiRequest
    ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : request;

  return next(authenticatedRequest).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && isApiRequest) {
        auth.handleUnauthorized();
      }
      return throwError(() => error);
    })
  );
};
