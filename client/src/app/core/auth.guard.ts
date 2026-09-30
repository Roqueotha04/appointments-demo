import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = (_, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (auth.token()) return true;

  return auth.refresh().pipe(
    map(() => true),
    catchError(() => of(router.createUrlTree(['/ingresar'], { queryParams: { returnUrl: state.url } }))),
  );
};
