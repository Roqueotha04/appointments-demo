import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './navbar.html',
  styleUrl: './navbar.css'
})
export class Navbar {
  readonly PHONE_NUMBER = '542236680996'; 

  get whatsappUrl(): string {
    const message = encodeURIComponent('Estimados, me contacto desde la plataforma de turnos para realizar una consulta.');
    return `https://wa.me/${this.PHONE_NUMBER}?text=${message}`;
  }
}