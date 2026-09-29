import { Component, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';

@Component({
  selector: 'app-contacto-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="form-container">
      <h2 class="title">Tus datos de contacto</h2>
      <p class="subtitle">Completá los datos para finalizar la reserva.</p>

      <form [formGroup]="contactoForm" (ngSubmit)="enviar()">
        <div class="field">
          <label>Email</label>
          <input type="email" formControlName="email" placeholder="ejemplo@correo.com">
          @if (contactoForm.get('email')?.invalid && contactoForm.get('email')?.touched) {
            <small class="error">Ingresá un email válido</small>
          }
        </div>

        <div class="field">
          <label>Teléfono</label>
          <input type="tel" formControlName="telefono" placeholder="223 4567890">
          @if (contactoForm.get('telefono')?.invalid && contactoForm.get('telefono')?.touched) {
            <small class="error">El teléfono es obligatorio</small>
          }
        </div>

        <button type="submit" class="btn-final" [disabled]="contactoForm.invalid">
          Confirmar Turno
        </button>
      </form>
    </div>
  `,
  styles: [`
    .form-container { max-width: 400px; margin: 0 auto; }
    .title { text-align: center; color: #2c3e50; margin-bottom: 0.5rem; }
    .subtitle { text-align: center; color: #7f8c8d; font-size: 0.9rem; margin-bottom: 2rem; }
    .field { margin-bottom: 1.5rem; display: flex; flex-direction: column; gap: 0.5rem; }
    .field label { font-weight: 600; color: #34495e; font-size: 0.9rem; }
    input { padding: 0.8rem; border: 1px solid #ddd; border-radius: 8px; font-size: 1rem; }
    input:focus { border-color: #2ecc71; outline: none; }
    .btn-final { width: 100%; padding: 1rem; background: #2ecc71; color: white; border: none; border-radius: 8px; font-weight: 600; cursor: pointer; }
    .btn-final:disabled { background: #eee; color: #999; cursor: not-allowed; }
    .error { color: #e74c3c; font-size: 0.75rem; }
  `]
})
export class ContactoForm {
  onConfirm = output<{ email: string; telefono: string }>();
  contactoForm: FormGroup;

  constructor(private fb: FormBuilder) {
    this.contactoForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      telefono: ['', [Validators.required, Validators.pattern('^[0-9]*$')]]
    });
  }

  enviar() {
    if (this.contactoForm.valid) {
      this.onConfirm.emit(this.contactoForm.value);
    }
  }
}