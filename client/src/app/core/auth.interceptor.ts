import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const token = auth.token();
  const outbound = req.clone({
    withCredentials: true,
    setHeaders: token ? { Authorization: `Bearer ${token}` } : {},
  });

  return next(outbound).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status !== 401 || req.url.includes('/api/auth/')) {
        return throwError(() => error);
      }

      return auth.refresh().pipe(
        switchMap((sesion) =>
          next(
            req.clone({
              withCredentials: true,
              setHeaders: { Authorization: `Bearer ${sesion.accessToken}` },
            }),
          ),
        ),
      );
    }),
  );
};

export function mensajeError(error: unknown): string {
  const http = error as HttpErrorResponse;
  return http?.error?.detail || http?.error?.title || 'No pudimos completar la acción.';
}
