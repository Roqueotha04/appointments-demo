import { Component, input, output } from '@angular/core';
import { DecimalPipe } from '@angular/common';

@Component({
  selector: 'app-servicio-selector',
  imports: [DecimalPipe],
  templateUrl: './servicio-selector.html',
  styleUrl: './servicio-selector.css',
})
export class ServicioSelector {
  servicios = input<any[]>([]);
  onSelect = output<any>();

  seleccionar(servicio: any) {
    this.onSelect.emit(servicio);
  }
}
