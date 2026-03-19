import { Component, input } from '@angular/core';
import { TurnoData } from '../../interfaces/interfaces';

@Component({
  selector: 'app-success-view',
  imports: [],
  templateUrl: './success-view.html',
  styleUrl: './success-view.css',
})
export class SuccessView {
info = input.required<TurnoData>();

  reiniciar() {
    window.location.reload();
  }
}
