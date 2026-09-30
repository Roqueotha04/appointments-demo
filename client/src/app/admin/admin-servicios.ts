import { HttpClient } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { mensajeError } from '../core/auth.interceptor';

@Component({
  selector: 'app-admin-servicios',
  imports: [ReactiveFormsModule],
  template: `
    <h1>Servicios</h1>
    <form [formGroup]="form" (ngSubmit)="crear()">
      <input formControlName="nombre" placeholder="Nombre" />
      <input formControlName="descripcion" placeholder="Descripción" />
      <input type="number" formControlName="duracionMinutos" placeholder="Minutos" />
      <input type="number" formControlName="precio" placeholder="Precio" />
      <button type="submit" [disabled]="form.invalid">Agregar</button>
    </form>
    @if (error()) { <p>{{ error() }}</p> }
    <ul>
      @for (servicio of servicios(); track servicio.id) {
        <li>
          {{ servicio.nombre }} · {{ servicio.duracionMinutos }} min · {{ servicio.precio }}
          @if (servicio.activo) { <button type="button" (click)="desactivar(servicio.id)">Desactivar</button> }
          @else { <span>inactivo</span> }
        </li>
      }
    </ul>
  `,
  styles: `h1 { font-family: Georgia, serif; font-weight: 500; } form { display: flex; gap: 0.5rem; flex-wrap: wrap; } input, button { padding: 0.6rem; border-radius: 8px; border: 1px solid #e8e2d6; }`,
})
export class AdminServicios implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);
  readonly servicios = signal<any[]>([]);
  readonly error = signal('');
  readonly form = inject(FormBuilder).nonNullable.group({
    nombre: ['', Validators.required],
    descripcion: [''],
    duracionMinutos: [30, [Validators.required, Validators.min(10)]],
    precio: [0, Validators.required],
  });

  ngOnInit() {
    this.cargar();
  }

  private id() {
    return this.route.snapshot.paramMap.get('id')!;
  }

  cargar() {
    this.http.get<any[]>(`/api/negocios/${this.id()}/servicios`).subscribe((servicios) => this.servicios.set(servicios));
  }

  crear() {
    this.http.post(`/api/negocios/${this.id()}/servicios`, this.form.getRawValue()).subscribe({
      next: () => {
        this.cargar();
        this.form.reset({ nombre: '', descripcion: '', duracionMinutos: 30, precio: 0 });
      },
      error: (error) => this.error.set(mensajeError(error)),
    });
  }

  desactivar(servicioId: string) {
    this.http.delete(`/api/negocios/${this.id()}/servicios/${servicioId}`).subscribe(() => this.cargar());
  }
}
