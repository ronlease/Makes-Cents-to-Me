import { Component, inject } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Institution } from '../../services/api.service';

export interface InstitutionDialogData {
  institution?: Institution;
}

export interface InstitutionDialogResult {
  name: string;
}

@Component({
  selector: 'app-institution-dialog',
  standalone: true,
  imports: [
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    ReactiveFormsModule,
  ],
  template: `
    <h2 mat-dialog-title>{{ data.institution ? 'Edit Institution' : 'Add Institution' }}</h2>
    <mat-dialog-content>
      <mat-form-field appearance="outline" class="full-width">
        <mat-label>Name</mat-label>
        <input matInput [formControl]="nameControl" placeholder="e.g. Chase, Bank of America" />
        @if (nameControl.hasError('required')) {
          <mat-error>Name is required.</mat-error>
        }
      </mat-form-field>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="cancel()">Cancel</button>
      <button mat-flat-button (click)="save()" [disabled]="nameControl.invalid">Save</button>
    </mat-dialog-actions>
  `,
  styles: [`
    .full-width { width: 100%; min-width: 320px; }
    mat-dialog-content { padding-top: 8px; }
  `],
})
export class InstitutionDialogComponent {
  protected readonly data = inject<InstitutionDialogData>(MAT_DIALOG_DATA);
  protected readonly nameControl = new FormControl<string>(
    this.data.institution?.name ?? '',
    { nonNullable: true, validators: [Validators.required] }
  );

  private readonly dialogRef = inject(MatDialogRef<InstitutionDialogComponent>);

  cancel(): void {
    this.dialogRef.close();
  }

  save(): void {
    if (this.nameControl.invalid) return;
    const result: InstitutionDialogResult = { name: this.nameControl.value };
    this.dialogRef.close(result);
  }
}
