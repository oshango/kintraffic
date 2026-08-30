import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';

import { SignalFormComponent } from './signal-form.component';

describe('SignalFormComponent', () => {
  let component: SignalFormComponent;
  let fixture: ComponentFixture<SignalFormComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ReactiveFormsModule, SignalFormComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(SignalFormComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize a valid form', () => {
    expect(component.form.get('name')?.value).toBe('');
    expect(component.form.valid).toBeFalsy();
  });

  it('should emit submitted data when valid', () => {
    const emitSpy = jest.spyOn(component.submitted, 'emit');

    component.form.patchValue({
      name: 'Main & 1st',
      status: 'normal',
      cycleTime: 45,
    });

    component.submit();

    expect(emitSpy).toHaveBeenCalledWith({
      name: 'Main & 1st',
      status: 'normal',
      cycleTime: 45,
    });
  });
});
