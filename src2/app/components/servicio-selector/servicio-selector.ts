import { Component, output, inject, signal, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-servicio-selector',
  standalone: true,
  templateUrl: './servicio-selector.html',
  styleUrl: './servicio-selector.css'
})
export class ServicioSelector implements OnInit {
  private http = inject(HttpClient);
  onSelect = output<any>();
  
  servicios = signal<any[]>([]);

  ngOnInit() {
    this.http.get<any[]>('http://localhost:3000/servicios')
      .subscribe(res => this.servicios.set(res));
  }

  seleccionar(s: any) {
    // Aquí podrías guardar 's' en un servicio global antes de emitir
    this.onSelect.emit(s);
  }
}