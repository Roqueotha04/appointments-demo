import { Component, output, inject, signal, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-calendario-horario',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './calendario-horario.html',
  styleUrl: './calendario-horario.css'
})
export class CalendarioHorario implements OnInit {
  private http = inject(HttpClient);
  onSelect = output<{fecha: string, hora: string}>();

  fechaSeleccionada = signal<string>('');
  horarios = signal<any[]>([]);
  horaSeleccionada = signal<string>('');

  ngOnInit() {
    this.http.get<any[]>('http://localhost:3000/horariosDisponibles')
      .subscribe(res => this.horarios.set(res));
  }

  seleccionarFecha(event: any) {
    this.fechaSeleccionada.set(event.target.value);
  }

  seleccionarHora(hora: string) {
    this.horaSeleccionada.set(hora);
  }

  confirmar() {
    if (this.fechaSeleccionada() && this.horaSeleccionada()) {
      this.onSelect.emit({
        fecha: this.fechaSeleccionada(),
        hora: this.horaSeleccionada()
      });
    }
  }
}