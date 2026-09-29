import { Injectable } from '@angular/core';
import { Observable, BehaviorSubject } from 'rxjs';
import { HttpClient } from '@angular/common/http';

@Injectable({ providedIn: 'root' })
export class TurnoService {
  private readonly API_URL = 'http://localhost:3000';
  
  private turnoSubject = new BehaviorSubject<any>({});
  turno$ = this.turnoSubject.asObservable();

  constructor(private http: HttpClient) {}

  getServicios(): Observable<any[]> {
    return this.http.get<any[]>(`${this.API_URL}/servicios`);
  }

  getHorarios(): Observable<any[]> {
    return this.http.get<any[]>(`${this.API_URL}/horariosDisponibles`);
  }

  actualizarTurno(data: any) {
    const current = this.turnoSubject.value;
    this.turnoSubject.next({ ...current, ...data });
  }
}
