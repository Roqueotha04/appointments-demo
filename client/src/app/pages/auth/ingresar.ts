import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { mensajeError } from '../../core/auth.interceptor';

@Component({
  selector: 'app-ingresar',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <section class="turno-card auth">
      <h1>Ingresar</h1>
      <form [formGroup]="form" (ngSubmit)="enviar()">
        <label>Email <input type="email" formControlName="email" /></label>
        <label>Contraseña <input type="password" formControlName="password" /></label>
        <button type="submit" [disabled]="form.invalid || enviando()">Entrar</button>
      </form>
      @if (error()) { <p class="error">{{ error() }}</p> }
      <a routerLink="/registro">Crear cuenta</a>
    </section>
  `,
  styles: `
    .auth { max-width: 420px; display: flex; flex-direction: column; gap: 1rem; }
    h1 { color: #4a3f35; font-family: Georgia, serif; font-weight: 500; }
    form, label { display: flex; flex-direction: column; gap: 0.4rem; }
    input { padding: 0.8rem; border: 1px solid #e8e2d6; border-radius: 8px; }
    button { background: #4a3f35; color: white; border: 0; border-radius: 999px; padding: 0.85rem; cursor: pointer; }
    .error { color: #9b3d3d; }
    a { color: #8c7e6d; }
  `,
})
export class Ingresar {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  readonly enviando = signal(false);
  readonly error = signal('');
  readonly form = inject(FormBuilder).nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  enviar() {
    this.enviando.set(true);
    this.error.set('');
    this.auth.login(this.form.getRawValue()).subscribe({
      next: () => this.router.navigateByUrl(this.route.snapshot.queryParamMap.get('returnUrl') || '/cuenta/turnos'),
      error: (error) => {
        this.error.set(mensajeError(error));
        this.enviando.set(false);
      },
    });
  }
}
