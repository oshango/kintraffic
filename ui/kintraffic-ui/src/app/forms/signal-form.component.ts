import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Output } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

export interface SignalFormValue {
  name: string;
  status: string;
  cycleTime: number;
}

@Component({
  selector: 'app-signal-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <form [formGroup]="form" (ngSubmit)="submit()">
      <label>
        Name
        <input formControlName="name" />
      </label>

      <label>
        Status
        <select formControlName="status">
          <option value="normal">Normal</option>
          <option value="warning">Warning</option>
          <option value="critical">Critical</option>
        </select>
      </label>

      <label>
        Cycle time
        <input type="number" formControlName="cycleTime" />
      </label>

      <button type="submit" [disabled]="form.invalid">Save</button>
    </form>
  `,
})
export class SignalFormComponent {
  @Output() submitted = new EventEmitter<SignalFormValue>();

  readonly form;

  constructor(private readonly fb: FormBuilder) {
    this.form = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(3)]],
      status: ['normal', Validators.required],
      cycleTime: [30, [Validators.required, Validators.min(1)]],
    });
  }

  submit(): void {
    if (this.form.valid) {
      this.submitted.emit(this.form.getRawValue() as SignalFormValue);
    }
  }
}
