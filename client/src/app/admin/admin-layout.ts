import { Component, inject, OnInit, signal } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';

@Component({
  selector: 'app-admin-layout',
  imports: [RouterOutlet, RouterLink],
  template: `
    <div class="admin" [class.abierto]="abierto()">
      <aside>
        <button type="button" class="cerrar" (click)="abierto.set(false)">Cerrar</button>
        <p class="marca">Panel</p>
        <a routerLink="/admin" (click)="abierto.set(false)">Negocios</a>
        @if (negocioId(); as id) {
          <a [routerLink]="['/admin/negocios', id, 'servicios']" (click)="abierto.set(false)">Servicios</a>
          <a [routerLink]="['/admin/negocios', id, 'empleados']" (click)="abierto.set(false)">Empleados</a>
          <a [routerLink]="['/admin/negocios', id, 'horarios']" (click)="abierto.set(false)">Horarios</a>
          <a [routerLink]="['/admin/negocios', id, 'turnos']" (click)="abierto.set(false)">Turnos</a>
        }
        <a routerLink="/">Volver al sitio</a>
      </aside>
      <div class="contenido">
        <button type="button" class="abrir" (click)="abierto.set(true)">Menú</button>
        <router-outlet />
      </div>
    </div>
  `,
  styles: `
    .admin { min-height: 100vh; display: grid; grid-template-columns: 240px 1fr; background: #fdfaf5; color: #4a3f35; }
    aside { border-right: 1px solid #e8e2d6; padding: 1.5rem; display: flex; flex-direction: column; gap: 0.8rem; }
    aside a { color: #4a3f35; text-decoration: none; }
    .marca { font-family: Georgia, serif; letter-spacing: 0.12em; text-transform: uppercase; }
    .contenido { padding: 1.5rem; }
    .abrir, .cerrar { display: none; background: transparent; border: 1px solid #e8e2d6; border-radius: 999px; padding: 0.4rem 0.8rem; }
    @media (max-width: 800px) {
      .admin { grid-template-columns: 1fr; }
      aside { display: none; }
      .admin.abierto aside { display: flex; }
      .abrir, .cerrar { display: inline-flex; }
    }
  `,
})
export class AdminLayout implements OnInit {
  private readonly router = inject(Router);
  readonly abierto = signal(false);
  readonly negocioId = signal<string | null>(null);

  ngOnInit() {
    this.router.events.pipe(filter((event) => event instanceof NavigationEnd)).subscribe(() => this.leerId());
    this.leerId();
  }

  private leerId() {
    const match = this.router.url.match(/negocios\/([^/]+)/);
    this.negocioId.set(match?.[1] ?? null);
  }
}
