import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-inicio',
  imports: [RouterLink],
  template: `
    <section class="turno-card inicio">
      <p class="eyebrow">Agenda para tu negocio</p>
      <h1>Tus clientes reservan. Vos definís quién atiende y cuándo.</h1>
      <p>Cada negocio tiene su link, sus servicios y su equipo. El cliente se registra, elige profesional y recibe la confirmación.</p>
      <div class="acciones">
        <a routerLink="/registro" class="btn">Crear mi negocio</a>
        <a routerLink="/estudio-norte" class="btn secundario">Ver Estudio Norte</a>
      </div>
    </section>
  `,
  styles: `
    .inicio { max-width: 720px; }
    .eyebrow { letter-spacing: 0.14em; text-transform: uppercase; color: #a69076; font-size: 0.75rem; }
    h1 { color: #4a3f35; font-family: Georgia, serif; font-weight: 500; font-size: 2.4rem; line-height: 1.2; }
    p { color: #8c7e6d; }
    .acciones { display: flex; gap: 0.8rem; flex-wrap: wrap; margin-top: 1.5rem; }
    .btn { background: #4a3f35; color: white; text-decoration: none; padding: 0.85rem 1.1rem; border-radius: 999px; }
    .secundario { background: transparent; color: #4a3f35; border: 1px solid #e8e2d6; }
  `,
})
export class Inicio {}
