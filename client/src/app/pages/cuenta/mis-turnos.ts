import { DatePipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { mensajeError } from '../../core/auth.interceptor';

@Component({
  selector: 'app-mis-turnos',
  imports: [DatePipe],
  template: `
    <section class="turno-card">
      <h1>Mis turnos</h1>
      @if (error()) { <p class="error">{{ error() }}</p> }
      @for (turno of turnos(); track turno.id) {
        <article>
          <strong>{{ turno.negocio }}</strong>
          <p>{{ turno.servicio }} con {{ turno.empleado }}</p>
          <p>{{ turno.inicioUtc | date:'full':'':'' }} · {{ turno.estado }}</p>
          @if (turno.estado === 'Confirmado') {
            <button type="button" (click)="cancelar(turno.id)">Cancelar</button>
            <label>Nueva fecha <input type="date" (change)="cargarHuecos(turno, $event)" /></label>
            @if (moviendo() === turno.id) {
              @for (hueco of huecos(); track hueco) {
                <button type="button" (click)="reprogramar(turno.id, hueco)">{{ hueco | date:'shortTime' }}</button>
              }
            }
          }
        </article>
      } @empty {
        <p>Todavía no tenés turnos.</p>
      }
    </section>
  `,
  styles: `
    h1 { color: #4a3f35; font-family: Georgia, serif; font-weight: 500; }
    article { border-top: 1px solid #e8e2d6; padding: 1rem 0; }
    button { margin-right: 0.5rem; }
    .error { color: #9b3d3d; }
  `,
})
export class MisTurnos implements OnInit {
  private readonly http = inject(HttpClient);
  readonly turnos = signal<any[]>([]);
  readonly huecos = signal<string[]>([]);
  readonly moviendo = signal<string | null>(null);
  readonly error = signal('');

  ngOnInit() {
    this.cargar();
  }

  cargar() {
    this.http.get<any[]>('/api/turnos/mios').subscribe((turnos) => this.turnos.set(turnos));
  }

  cancelar(id: string) {
    this.http.post(`/api/turnos/${id}/cancelar`, {}).subscribe({
      next: () => this.cargar(),
      error: (error) => this.error.set(mensajeError(error)),
    });
  }

  cargarHuecos(turno: any, event: Event) {
    const fecha = (event.target as HTMLInputElement).value;
    this.moviendo.set(turno.id);
    this.http
      .get<{ iniciosUtc: string[] }>(`/api/publico/negocios/${turno.negocioSlug}/huecos`, {
        params: { servicioId: turno.servicioId, empleadoId: turno.empleadoId, fecha },
      })
      .subscribe((respuesta) => this.huecos.set(respuesta.iniciosUtc));
  }

  reprogramar(id: string, inicioUtc: string) {
    this.http.post(`/api/turnos/${id}/reprogramar`, { inicioUtc }).subscribe({
      next: () => {
        this.moviendo.set(null);
        this.cargar();
      },
      error: (error) => this.error.set(mensajeError(error)),
    });
  }
}
