import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ServicioSelector } from '../../components/servicio-selector/servicio-selector';
import { StepIndicator } from '../../components/step-indicator/step-indicator';
// Importar los demás según los vayas creando

@Component({
  selector: 'app-turno-page',
  standalone: true,
  imports: [CommonModule, StepIndicator, ServicioSelector],
  templateUrl: './turno-page.html',
  styleUrl: './turno-page.css',
})
export class TurnoPage {
  pasoActual = signal(1);

  avanzar() {
    this.pasoActual.update(p => p + 1);
  }

  volver() {
    this.pasoActual.update(p => p - 1);
  }
}