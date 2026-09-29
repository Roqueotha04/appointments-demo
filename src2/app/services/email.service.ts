import { Injectable } from '@angular/core';
import emailjs from '@emailjs/browser';
import { environment } from '../../enviroments/enviroment';
import { TurnoData } from '../interfaces/interfaces';

@Injectable({
  providedIn: 'root'
})
export class EmailService {

  async enviarConfirmacion(datos: TurnoData) {
    const templateParams = {
      name: datos.email, // El remitente o cliente
      message: datos.servicio?.nombre, // El servicio elegido
      time: `${datos.fecha} a las ${datos.hora}hs`, // Fecha y hora combinadas
      to_email: datos.email // A quién le llega el mail
    };

    try {
      return await emailjs.send(
        environment.emailjs_service_id,
        environment.emailjs_template_id,
        templateParams,
        environment.emailjs_public_key
      );
    } catch (error) {
      console.error('Error de EmailJS:', error);
      throw error;
    }
  }
}