import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

import { Servicio, TurnoData } from '../../interfaces/interfaces';

import { EmailService } from '../../services/email.service';
import { StepIndicator } from '../../components/step-indicator/step-indicator';
import { ServicioSelector } from '../../components/servicio-selector/servicio-selector';
import { CalendarioHorario } from '../../components/calendario-horario/calendario-horario';
import { ContactoForm } from '../../components/contacto-form/contacto-form';
import { SuccessView } from '../../components/success-view/success-view';


@Component({
  selector: 'app-turno-page',
  standalone: true,
  imports: [
    CommonModule,
    StepIndicator,
    ServicioSelector,
    CalendarioHorario,
    ContactoForm,
    SuccessView
  ],
  templateUrl: './turno-page.html',
  styleUrl: './turno-page.css'
})
export class TurnoPage {
  pasoActual = signal(1);
  datosTurno = signal<TurnoData>({});
  cargando = signal(false);

  constructor(private emailService: EmailService) {}

  handleServicioSelected(servicio: Servicio) {
    this.datosTurno.update(prev => ({ ...prev, servicio }));
    this.avanzar();
  }

  handleFechaHoraSelected(inicioUtc: string) {
    this.datosTurno.update(prev => ({ ...prev, fecha: inicioUtc, hora: inicioUtc }));
    this.avanzar();
  }

  async handleFinalConfirm(contacto: { email: string; telefono: string }) {
    this.datosTurno.update(prev => ({ ...prev, ...contacto }));
    
    this.cargando.set(true);

    try {
      await this.emailService.enviarConfirmacion(this.datosTurno());
      console.log('Notificación enviada con éxito');
    } catch (error) {
      console.error('Error al enviar la notificación:', error);
    } finally {
      this.cargando.set(false);
      this.avanzar();
    }
  }

  avanzar() {
    this.pasoActual.update(p => p + 1);
  }

  volver() {
    if (this.pasoActual() > 1) {
      this.pasoActual.update(p => p - 1);
    }
  }
}