import { DatePipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { mensajeError } from '../core/auth.interceptor';

@Component({
  selector: 'app-admin-turnos',
  imports: [DatePipe],
  template: `
    <h1>Turnos</h1>
    @if (error()) { <p>{{ error() }}</p> }
    @for (turno of turnos(); track turno.id) {
      <article>
        <strong>{{ turno.cliente }}</strong>
        <span> {{ turno.clienteEmail }}</span>
        <p>{{ turno.servicio }} con {{ turno.empleado }}</p>
        <p>{{ turno.inicioUtc | date:'full' }} · {{ turno.estado }}</p>
        @if (turno.estado === 'Confirmado') {
          <button type="button" (click)="cancelar(turno.id)">Cancelar</button>
        }
      </article>
    } @empty { <p>No hay turnos.</p> }
  `,
  styles: `h1 { font-family: Georgia, serif; font-weight: 500; } article { border-top: 1px solid #e8e2d6; padding: 0.8rem 0; }`,
})
export class AdminTurnos implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);
  readonly turnos = signal<any[]>([]);
  readonly error = signal('');

  ngOnInit() {
    this.cargar();
  }

  cargar() {
    this.http.get<any[]>(`/api/negocios/${this.route.snapshot.paramMap.get('id')}/turnos`).subscribe((turnos) => this.turnos.set(turnos));
  }

  cancelar(id: string) {
    this.http.post(`/api/turnos/${id}/cancelar`, {}).subscribe({
      next: () => this.cargar(),
      error: (error) => this.error.set(mensajeError(error)),
    });
  }
}
