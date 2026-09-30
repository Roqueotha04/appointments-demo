import { HttpClient } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { mensajeError } from '../core/auth.interceptor';

@Component({
  selector: 'app-admin-horarios',
  imports: [FormsModule],
  template: `
    <h1>Horarios</h1>
    <label>
      Empleado
      <select [(ngModel)]="empleadoId" (ngModelChange)="cargar()">
        @for (empleado of empleados(); track empleado.id) {
          <option [value]="empleado.id">{{ empleado.nombre }}</option>
        }
      </select>
    </label>
    @for (franja of franjas(); track $index) {
      <div>
        <select [(ngModel)]="franja.diaSemana">
          <option [ngValue]="1">Lunes</option>
          <option [ngValue]="2">Martes</option>
          <option [ngValue]="3">Miércoles</option>
          <option [ngValue]="4">Jueves</option>
          <option [ngValue]="5">Viernes</option>
          <option [ngValue]="6">Sábado</option>
          <option [ngValue]="0">Domingo</option>
        </select>
        <input type="time" [(ngModel)]="franja.horaInicio" />
        <input type="time" [(ngModel)]="franja.horaFin" />
      </div>
    }
    <button type="button" (click)="agregar()">Agregar franja</button>
    <button type="button" (click)="guardar()">Guardar horario</button>
    <h2>Bloqueo</h2>
    <input type="date" [(ngModel)]="bloqueo.fecha" />
    <input type="time" [(ngModel)]="bloqueo.horaInicio" />
    <input type="time" [(ngModel)]="bloqueo.horaFin" />
    <button type="button" (click)="bloquear()">Bloquear</button>
    @if (error()) { <p>{{ error() }}</p> }
  `,
  styles: `h1, h2 { font-family: Georgia, serif; font-weight: 500; } div { display: flex; gap: 0.4rem; margin: 0.4rem 0; flex-wrap: wrap; }`,
})
export class AdminHorarios implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);
  readonly empleados = signal<any[]>([]);
  readonly franjas = signal<{ diaSemana: number; horaInicio: string; horaFin: string }[]>([]);
  readonly error = signal('');
  empleadoId = '';
  bloqueo = { fecha: '', horaInicio: '10:00', horaFin: '12:00' };

  ngOnInit() {
    this.http.get<any[]>(`/api/negocios/${this.id()}/empleados`).subscribe((empleados) => {
      this.empleados.set(empleados.filter((e) => e.activo));
      this.empleadoId = empleados[0]?.id ?? '';
      if (this.empleadoId) this.cargar();
    });
  }

  private id() {
    return this.route.snapshot.paramMap.get('id')!;
  }

  cargar() {
    this.http
      .get<any[]>(`/api/negocios/${this.id()}/empleados/${this.empleadoId}/disponibilidad`)
      .subscribe((franjas) => this.franjas.set(franjas));
  }

  agregar() {
    this.franjas.update((franjas) => [...franjas, { diaSemana: 1, horaInicio: '10:00', horaFin: '18:00' }]);
  }

  guardar() {
    this.http
      .put(`/api/negocios/${this.id()}/empleados/${this.empleadoId}/disponibilidad`, { franjas: this.franjas() })
      .subscribe({ error: (error) => this.error.set(mensajeError(error)) });
  }

  bloquear() {
    this.http
      .post(`/api/negocios/${this.id()}/empleados/${this.empleadoId}/bloqueos`, this.bloqueo)
      .subscribe({
        next: () => this.error.set(''),
        error: (error) => this.error.set(mensajeError(error)),
      });
  }
}
