import { HttpClient } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { StepIndicator } from '../../components/step-indicator/step-indicator';
import { ServicioSelector } from '../../components/servicio-selector/servicio-selector';
import { CalendarioHorario } from '../../components/calendario-horario/calendario-horario';
import { AuthService } from '../../core/auth.service';
import { mensajeError } from '../../core/auth.interceptor';

@Component({
  selector: 'app-reserva-page',
  imports: [StepIndicator, ServicioSelector, CalendarioHorario, RouterLink],
  templateUrl: './reserva-page.html',
  styleUrl: './reserva-page.css',
})
export class ReservaPage implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  readonly auth = inject(AuthService);

  readonly paso = signal(1);
  readonly negocio = signal<any>(null);
  readonly servicios = signal<any[]>([]);
  readonly empleados = signal<any[]>([]);
  readonly huecos = signal<{ inicioUtc: string; etiqueta: string }[]>([]);
  readonly servicio = signal<any>(null);
  readonly empleado = signal<any>(null);
  readonly inicioUtc = signal('');
  readonly error = signal('');
  readonly listo = signal(false);

  ngOnInit() {
    const slug = this.route.snapshot.paramMap.get('slug');
    this.http.get(`/api/publico/negocios/${slug}`).subscribe({
      next: (negocio) => {
        this.negocio.set(negocio);
        this.http.get<any[]>(`/api/publico/negocios/${slug}/servicios`).subscribe((items) => this.servicios.set(items));
      },
      error: () => this.error.set('No encontramos ese negocio.'),
    });
  }

  elegirServicio(servicio: any) {
    this.servicio.set(servicio);
    this.http
      .get<any[]>(`/api/publico/negocios/${this.negocio().slug}/servicios/${servicio.id}/empleados`)
      .subscribe((empleados) => {
        this.empleados.set(empleados);
        if (empleados.length === 1) {
          this.empleado.set(empleados[0]);
          this.paso.set(3);
        } else {
          this.paso.set(2);
        }
      });
  }

  elegirEmpleado(empleado: any) {
    this.empleado.set(empleado);
    this.paso.set(3);
  }

  cargarHuecos(fecha: string) {
    const negocio = this.negocio();
    this.http
      .get<{ iniciosUtc: string[] }>(`/api/publico/negocios/${negocio.slug}/huecos`, {
        params: { servicioId: this.servicio().id, empleadoId: this.empleado().id, fecha },
      })
      .subscribe((respuesta) => {
        this.huecos.set(
          respuesta.iniciosUtc.map((inicioUtc) => ({
            inicioUtc,
            etiqueta: new Intl.DateTimeFormat('es-AR', {
              hour: '2-digit',
              minute: '2-digit',
              timeZone: negocio.zonaHoraria,
            }).format(new Date(inicioUtc)),
          })),
        );
      });
  }

  elegirHorario(inicioUtc: string) {
    this.inicioUtc.set(inicioUtc);
    this.paso.set(4);
  }

  confirmar() {
    if (!this.auth.token()) {
      this.router.navigate(['/ingresar'], { queryParams: { returnUrl: this.router.url } });
      return;
    }

    this.error.set('');
    this.http
      .post('/api/turnos', {
        negocioId: this.negocio().id,
        servicioId: this.servicio().id,
        empleadoId: this.empleado().id,
        inicioUtc: this.inicioUtc(),
      })
      .subscribe({
        next: () => this.listo.set(true),
        error: (error) => this.error.set(mensajeError(error)),
      });
  }

  horaElegida() {
    const inicio = this.inicioUtc();
    if (!inicio || !this.negocio()) return '';
    return new Intl.DateTimeFormat('es-AR', {
      dateStyle: 'full',
      timeStyle: 'short',
      timeZone: this.negocio().zonaHoraria,
    }).format(new Date(inicio));
  }

  volver() {
    this.paso.update((paso) => Math.max(1, paso - 1));
  }
}
