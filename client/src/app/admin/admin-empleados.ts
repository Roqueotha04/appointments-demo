import { HttpClient } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { mensajeError } from '../core/auth.interceptor';

@Component({
  selector: 'app-admin-empleados',
  imports: [ReactiveFormsModule],
  template: `
    <h1>Empleados</h1>
    <form [formGroup]="form" (ngSubmit)="crear()">
      <input formControlName="nombre" placeholder="Nombre" />
      <button type="submit" [disabled]="form.invalid">Agregar</button>
    </form>
    @if (error()) { <p>{{ error() }}</p> }
    @for (empleado of empleados(); track empleado.id) {
      <article>
        <strong>{{ empleado.nombre }}</strong>
        @if (!empleado.activo) { <span> inactivo</span> }
        <div>
          @for (servicio of servicios(); track servicio.id) {
            <label>
              <input type="checkbox" [checked]="empleado.servicioIds.includes(servicio.id)" (change)="toggle(empleado, servicio.id)" />
              {{ servicio.nombre }}
            </label>
          }
        </div>
        <button type="button" (click)="guardar(empleado)">Guardar servicios</button>
        @if (empleado.activo) { <button type="button" (click)="desactivar(empleado.id)">Desactivar</button> }
      </article>
    }
  `,
  styles: `h1 { font-family: Georgia, serif; font-weight: 500; } article { border-top: 1px solid #e8e2d6; padding: 1rem 0; } label { display: block; }`,
})
export class AdminEmpleados implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);
  readonly empleados = signal<any[]>([]);
  readonly servicios = signal<any[]>([]);
  readonly error = signal('');
  readonly form = inject(FormBuilder).nonNullable.group({ nombre: ['', Validators.required] });

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.http.get<any[]>(`/api/negocios/${id}/servicios`).subscribe((servicios) => this.servicios.set(servicios.filter((s) => s.activo)));
    this.cargar();
  }

  private id() {
    return this.route.snapshot.paramMap.get('id')!;
  }

  cargar() {
    this.http.get<any[]>(`/api/negocios/${this.id()}/empleados`).subscribe((empleados) => this.empleados.set(empleados));
  }

  crear() {
    this.http.post(`/api/negocios/${this.id()}/empleados`, this.form.getRawValue()).subscribe({
      next: () => {
        this.form.reset();
        this.cargar();
      },
      error: (error) => this.error.set(mensajeError(error)),
    });
  }

  toggle(empleado: any, servicioId: string) {
    const ids = new Set<string>(empleado.servicioIds);
    ids.has(servicioId) ? ids.delete(servicioId) : ids.add(servicioId);
    empleado.servicioIds = [...ids];
  }

  guardar(empleado: any) {
    this.http
      .put(`/api/negocios/${this.id()}/empleados/${empleado.id}/servicios`, { servicioIds: empleado.servicioIds })
      .subscribe({ error: (error) => this.error.set(mensajeError(error)) });
  }

  desactivar(empleadoId: string) {
    this.http.delete(`/api/negocios/${this.id()}/empleados/${empleadoId}`).subscribe(() => this.cargar());
  }
}
