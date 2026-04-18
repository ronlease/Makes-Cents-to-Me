import { Component, inject } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Category } from '../../services/api.service';

export interface CategoryDialogData {
  category?: Category;
}

export interface CategoryDialogResult {
  name: string;
}

@Component({
  selector: 'app-category-dialog',
  standalone: true,
  imports: [
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    ReactiveFormsModule,
  ],
  template: `
    <h2 mat-dialog-title>{{ data.category ? 'Edit Category' : 'Add Category' }}</h2>
    <mat-dialog-content>
      <mat-form-field appearance="outline" class="full-width">
        <mat-label>Name</mat-label>
        <input matInput [formControl]="nameControl" placeholder="e.g. Groceries, Utilities" />
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
export class CategoryDialogComponent {
  protected readonly data = inject<CategoryDialogData>(MAT_DIALOG_DATA);
  protected readonly nameControl = new FormControl<string>(
    this.data.category?.name ?? '',
    { nonNullable: true, validators: [Validators.required] }
  );

  private readonly dialogRef = inject(MatDialogRef<CategoryDialogComponent>);

  cancel(): void {
    this.dialogRef.close();
  }

  save(): void {
    if (this.nameControl.invalid) return;
    const result: CategoryDialogResult = { name: this.nameControl.value };
    this.dialogRef.close(result);
  }
}
