import { Component, input } from '@angular/core';

@Component({
  selector: 'app-step-indicator',
  standalone: true,
  template: `
    <div class="stepper">
      @for (step of steps; track step) {
        <div class="step" [class.active]="currentStep() >= ($index + 1)">
          <div class="circle">{{ $index + 1 }}</div>
          <span>{{ step }}</span>
        </div>
        @if (!$last) { <div class="line"></div> }
      }
    </div>
  `,
  styleUrl: './step-indicator.css'
})
export class StepIndicator {
  currentStep = input.required<number>();
  steps = ['Servicio', 'Fecha y Hora', 'Tus Datos'];
}