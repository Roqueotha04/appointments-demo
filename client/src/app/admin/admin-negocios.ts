import { HttpClient } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { mensajeError } from '../core/auth.interceptor';

@Component({
  selector: 'app-admin-negocios',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <h1>Negocios</h1>
    <form [formGroup]="form" (ngSubmit)="crear()">
      <input formControlName="nombre" placeholder="Nombre del negocio" />
      <button type="submit" [disabled]="form.invalid">Crear</button>
    </form>
    @if (error()) { <p>{{ error() }}</p> }
    <ul>
      @for (negocio of negocios(); track negocio.id) {
        <li>
          <a [routerLink]="['/admin/negocios', negocio.id, 'servicios']">{{ negocio.nombre }}</a>
          <span>/{{ negocio.slug }}</span>
        </li>
      }
    </ul>
  `,
  styles: `
    h1 { font-family: Georgia, serif; font-weight: 500; }
    form { display: flex; gap: 0.5rem; flex-wrap: wrap; }
    input, button { padding: 0.7rem; border-radius: 8px; border: 1px solid #e8e2d6; }
    button { background: #4a3f35; color: white; }
    li { margin: 0.8rem 0; }
    a { color: #4a3f35; }
  `,
})
export class AdminNegocios implements OnInit {
  private readonly http = inject(HttpClient);
  readonly negocios = signal<any[]>([]);
  readonly error = signal('');
  readonly form = inject(FormBuilder).nonNullable.group({ nombre: ['', Validators.required] });

  ngOnInit() {
    this.cargar();
  }

  cargar() {
    this.http.get<any[]>('/api/negocios').subscribe((negocios) => this.negocios.set(negocios));
  }

  crear() {
    this.http.post('/api/negocios', { nombre: this.form.controls.nombre.value }).subscribe({
      next: () => {
        this.form.reset();
        this.cargar();
      },
      error: (error) => this.error.set(mensajeError(error)),
    });
  }
}
