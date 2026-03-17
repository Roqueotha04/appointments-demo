import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SuccessView } from './success-view';

describe('SuccessView', () => {
  let component: SuccessView;
  let fixture: ComponentFixture<SuccessView>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SuccessView]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SuccessView);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
