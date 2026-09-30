import { Component, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-navbar',
  imports: [RouterLink],
  templateUrl: './navbar.html',
  styleUrl: './navbar.css',
})
export class Navbar {
  readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  salir() {
    this.auth.logout().subscribe(() => this.router.navigateByUrl('/'));
  }
}
