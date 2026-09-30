import { Routes } from '@angular/router';
import { authGuard } from './core/auth.guard';
import { PublicLayout } from './layout/public-layout';
import { Inicio } from './pages/inicio/inicio';
import { Ingresar } from './pages/auth/ingresar';
import { Registro } from './pages/auth/registro';
import { ReservaPage } from './pages/reserva/reserva-page';
import { MisTurnos } from './pages/cuenta/mis-turnos';
import { AdminLayout } from './admin/admin-layout';
import { AdminNegocios } from './admin/admin-negocios';
import { AdminServicios } from './admin/admin-servicios';
import { AdminEmpleados } from './admin/admin-empleados';
import { AdminHorarios } from './admin/admin-horarios';
import { AdminTurnos } from './admin/admin-turnos';

export const routes: Routes = [
  {
    path: '',
    component: PublicLayout,
    children: [
      { path: '', component: Inicio },
      { path: 'ingresar', component: Ingresar },
      { path: 'registro', component: Registro },
      { path: 'cuenta/turnos', component: MisTurnos, canActivate: [authGuard] },
      { path: ':slug', component: ReservaPage },
    ],
  },
  {
    path: 'admin',
    component: AdminLayout,
    canActivate: [authGuard],
    children: [
      { path: '', component: AdminNegocios },
      { path: 'negocios/:id/servicios', component: AdminServicios },
      { path: 'negocios/:id/empleados', component: AdminEmpleados },
      { path: 'negocios/:id/horarios', component: AdminHorarios },
      { path: 'negocios/:id/turnos', component: AdminTurnos },
    ],
  },
];
