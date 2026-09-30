import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Navbar } from '../components/navbar/navbar';
import { Footer } from '../components/footer/footer';

@Component({
  selector: 'app-public-layout',
  imports: [RouterOutlet, Navbar, Footer],
  template: `
    <div class="main-layout">
      <app-navbar />
      <main class="content-area">
        <router-outlet />
      </main>
      <app-footer />
    </div>
  `,
})
export class PublicLayout {}
