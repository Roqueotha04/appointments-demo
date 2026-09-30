import { Component, input, output, signal } from '@angular/core';

@Component({
  selector: 'app-calendario-horario',
  templateUrl: './calendario-horario.html',
  styleUrl: './calendario-horario.css',
})
export class CalendarioHorario {
  huecos = input<{ inicioUtc: string; etiqueta: string }[]>([]);
  fechaChange = output<string>();
  onSelect = output<string>();

  fechaSeleccionada = signal('');
  elegido = signal('');

  seleccionarFecha(event: Event) {
    const fecha = (event.target as HTMLInputElement).value;
    this.fechaSeleccionada.set(fecha);
    this.elegido.set('');
    this.fechaChange.emit(fecha);
  }

  seleccionarHora(inicioUtc: string) {
    this.elegido.set(inicioUtc);
  }

  confirmar() {
    if (this.elegido()) this.onSelect.emit(this.elegido());
  }
}
