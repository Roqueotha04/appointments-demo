import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ContactoForm } from './contacto-form';

describe('ContactoForm', () => {
  let component: ContactoForm;
  let fixture: ComponentFixture<ContactoForm>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ContactoForm]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ContactoForm);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
