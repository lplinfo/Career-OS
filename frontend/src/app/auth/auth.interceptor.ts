import { inject } from '@angular/core';
import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthSessionService } from './auth-session.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthSessionService);
  const router = inject(Router);

  const token = authService.getToken();
  const isApiRequest = req.url.includes('/api/');
  const isAuthEndpoint = req.url.includes('/api/auth/login') ||
                         req.url.includes('/api/auth/register') ||
                         req.url.includes('/api/auth/exchange-google') ||
                         req.url.includes('/api/auth/login-google');

  let authReq = req;

  if (token && isApiRequest && !isAuthEndpoint) {
    authReq = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        authService.clearSession();
        router.navigate(['/']);
      }
      return throwError(() => error);
    })
  );
};
