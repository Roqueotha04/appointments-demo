import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, finalize, shareReplay, tap } from 'rxjs';

export interface Usuario {
  id: string;
  nombre: string;
  email: string;
  telefono?: string;
}

export interface Sesion {
  accessToken: string;
  expiraUtc: string;
  usuario: Usuario;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly accessToken = signal<string | null>(null);
  private refresco: Observable<Sesion> | null = null;
  readonly usuario = signal<Usuario | null>(null);

  token(): string | null {
    return this.accessToken();
  }

  registro(body: { nombre: string; email: string; password: string; telefono?: string }): Observable<Sesion> {
    return this.guardar(this.http.post<Sesion>('/api/auth/registro', body));
  }

  login(body: { email: string; password: string }): Observable<Sesion> {
    return this.guardar(this.http.post<Sesion>('/api/auth/login', body));
  }

  refresh(): Observable<Sesion> {
    this.refresco ??= this.guardar(this.http.post<Sesion>('/api/auth/refresh', {})).pipe(
      finalize(() => {
        this.refresco = null;
      }),
      shareReplay(1),
    );
    return this.refresco;
  }

  logout(): Observable<void> {
    return this.http.post<void>('/api/auth/logout', {}).pipe(
      tap(() => {
        this.accessToken.set(null);
        this.usuario.set(null);
      }),
    );
  }

  private guardar(request: Observable<Sesion>): Observable<Sesion> {
    return request.pipe(
      tap((sesion) => {
        this.accessToken.set(sesion.accessToken);
        this.usuario.set(sesion.usuario);
      }),
    );
  }
}
