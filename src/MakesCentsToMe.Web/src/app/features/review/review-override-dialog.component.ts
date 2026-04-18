import { Component, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import {
  MAT_DIALOG_DATA,
  MatDialogModule,
  MatDialogRef,
} from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { Category, ReviewTransaction } from '../../services/api.service';

export interface ReviewOverrideDialogData {
  categories: Category[];
  transaction: ReviewTransaction;
}

export interface ReviewOverrideDialogResult {
  categoryId: string | null;
  normalizedVendor: string;
}

@Component({
  selector: 'app-review-override-dialog',
  standalone: true,
  imports: [
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    ReactiveFormsModule,
  ],
  template: `
    <h2 mat-dialog-title>Override Transaction</h2>

    <mat-dialog-content>
      <p class="transaction-description">{{ data.transaction.description }}</p>

      <form [formGroup]="overrideForm" class="override-form">
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Normalized Vendor</mat-label>
          <input matInput formControlName="normalizedVendor" placeholder="e.g. Amazon" />
          @if (overrideForm.controls.normalizedVendor.hasError('required')) {
            <mat-error>Vendor name is required.</mat-error>
          }
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Category</mat-label>
          <mat-select formControlName="categoryId">
            <mat-option [value]="null">— None —</mat-option>
            @for (category of data.categories; track category.id) {
              <mat-option [value]="category.id">{{ category.name }}</mat-option>
            }
          </mat-select>
        </mat-form-field>
      </form>
    </mat-dialog-content>

    <mat-dialog-actions align="end">
      <button mat-button (click)="cancel()">Cancel</button>
      <button
        mat-flat-button
        color="primary"
        [disabled]="overrideForm.invalid"
        (click)="save()">
        Save Override
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    .full-width { width: 100%; }
    .override-form { display: flex; flex-direction: column; gap: 8px; padding-top: 8px; }
    .transaction-description {
      color: var(--mat-sys-on-surface-variant);
      font-size: 0.875rem;
      margin: 0 0 16px;
    }
  `],
})
export class ReviewOverrideDialogComponent {
  protected readonly data: ReviewOverrideDialogData = inject(MAT_DIALOG_DATA);

  protected readonly overrideForm = new FormGroup({
    categoryId: new FormControl<string | null>(
      this.data.transaction.suggestedCategoryId ?? null
    ),
    normalizedVendor: new FormControl<string>(
      this.data.transaction.suggestedNormalizedVendor ?? '',
      { nonNullable: true, validators: [Validators.required] }
    ),
  });

  private readonly dialogRef = inject(MatDialogRef<ReviewOverrideDialogComponent>);

  cancel(): void {
    this.dialogRef.close(undefined);
  }

  save(): void {
    if (this.overrideForm.invalid) return;
    const result: ReviewOverrideDialogResult = {
      categoryId: this.overrideForm.controls.categoryId.value ?? null,
      normalizedVendor: this.overrideForm.controls.normalizedVendor.value,
    };
    this.dialogRef.close(result);
  }
}
