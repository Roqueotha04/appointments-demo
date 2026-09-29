export interface Servicio {
  id: number;
  nombre: string;
  precio: number;
  duracion: string;
  descripcion: string;
}

export interface TurnoData {
  servicio?: Servicio; 
  fecha?: string; 
  hora?: string;
  email?: string;
  telefono?: string;
}