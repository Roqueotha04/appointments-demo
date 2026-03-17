import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { TurnoPage } from "./pages/turno-page/turno-page";
import { Navbar } from "./components/navbar/navbar";
import { Footer } from "./components/footer/footer";

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, TurnoPage, Navbar, Footer],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('demo-turnos');
}
