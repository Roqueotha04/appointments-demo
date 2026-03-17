import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CalendarioHorario } from './calendario-horario';

describe('CalendarioHorario', () => {
  let component: CalendarioHorario;
  let fixture: ComponentFixture<CalendarioHorario>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CalendarioHorario]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CalendarioHorario);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
