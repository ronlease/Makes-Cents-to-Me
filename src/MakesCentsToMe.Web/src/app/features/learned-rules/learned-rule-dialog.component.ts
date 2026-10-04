import { Component, inject } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { Category, LearnedRule, LearnedRuleSuggestion } from '../../services/api.service';

export interface LearnedRuleDialogData {
  categories: Category[];
  mode: 'create' | 'edit' | 'suggestion';
  rule?: LearnedRule;
  suggestion?: LearnedRuleSuggestion;
}

export interface LearnedRuleDialogResult {
  categoryId: string;
  normalizedVendor: string;
  pattern: string;
}

@Component({
  selector: 'app-learned-rule-dialog',
  standalone: true,
  imports: [
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    ReactiveFormsModule,
  ],
  template: `
    <h2 mat-dialog-title>{{ title }}</h2>
    <mat-dialog-content>
      @if (data.mode === 'suggestion' && data.suggestion) {
        <p class="explanation">
          Future transactions starting with <strong>{{ patternControl.value }}</strong> will be
          categorized as <strong>{{ vendorControl.value }}</strong> /
          <strong>{{ selectedCategoryName }}</strong> without Claude.
        </p>
      }
      <mat-form-field appearance="outline" class="full-width">
        <mat-label>Pattern</mat-label>
        <input matInput [formControl]="patternControl" placeholder="e.g. WHOLEFDS MKT" />
        <mat-hint>Matches transactions whose description starts with this text.</mat-hint>
        @if (patternControl.hasError('required')) {
          <mat-error>Pattern is required.</mat-error>
        } @else if (patternControl.hasError('minlength')) {
          <mat-error>Pattern must be at least 3 characters.</mat-error>
        }
      </mat-form-field>
      <mat-form-field appearance="outline" class="full-width">
        <mat-label>Vendor</mat-label>
        <input matInput [formControl]="vendorControl" placeholder="e.g. Whole Foods" />
        @if (vendorControl.hasError('required')) {
          <mat-error>Vendor is required.</mat-error>
        }
      </mat-form-field>
      <mat-form-field appearance="outline" class="full-width">
        <mat-label>Category</mat-label>
        <mat-select [formControl]="categoryControl">
          @for (category of data.categories; track category.id) {
            <mat-option [value]="category.id">{{ category.name }}</mat-option>
          }
        </mat-select>
        @if (categoryControl.hasError('required')) {
          <mat-error>Category is required.</mat-error>
        }
      </mat-form-field>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="cancel()">{{ data.mode === 'suggestion' ? 'Not now' : 'Cancel' }}</button>
      <button mat-flat-button (click)="save()" [disabled]="isInvalid">{{ confirmLabel }}</button>
    </mat-dialog-actions>
  `,
  styles: [
    `
      .explanation {
        margin-top: 0;
      }
      .full-width {
        width: 100%;
        min-width: 360px;
      }
      mat-dialog-content {
        padding-top: 8px;
      }
    `,
  ],
})
export class LearnedRuleDialogComponent {
  protected readonly data = inject<LearnedRuleDialogData>(MAT_DIALOG_DATA);
  protected readonly categoryControl = new FormControl<string>(
    this.data.rule?.categoryId ?? this.data.suggestion?.categoryId ?? '',
    { nonNullable: true, validators: [Validators.required] },
  );
  protected readonly patternControl = new FormControl<string>(
    this.data.rule?.pattern ?? this.data.suggestion?.pattern ?? '',
    { nonNullable: true, validators: [Validators.required, Validators.minLength(3)] },
  );
  protected readonly vendorControl = new FormControl<string>(
    this.data.rule?.normalizedVendor ?? this.data.suggestion?.normalizedVendor ?? '',
    { nonNullable: true, validators: [Validators.required] },
  );

  private readonly dialogRef = inject(MatDialogRef<LearnedRuleDialogComponent>);

  protected get confirmLabel(): string {
    if (this.data.mode === 'suggestion') {
      return this.data.suggestion?.existingLearnedRuleId ? 'Update rule' : 'Create rule';
    }
    return 'Save';
  }

  protected get isInvalid(): boolean {
    return this.patternControl.invalid || this.vendorControl.invalid || this.categoryControl.invalid;
  }

  protected get selectedCategoryName(): string {
    return (
      this.data.categories.find((category) => category.id === this.categoryControl.value)?.name ??
      this.data.suggestion?.categoryName ??
      ''
    );
  }

  protected get title(): string {
    switch (this.data.mode) {
      case 'create':
        return 'Add Learned Rule';
      case 'edit':
        return 'Edit Learned Rule';
      default:
        return this.data.suggestion?.existingLearnedRuleId
          ? 'Update the learned rule?'
          : 'Create a learned rule?';
    }
  }

  cancel(): void {
    this.dialogRef.close();
  }

  save(): void {
    if (this.isInvalid) return;
    const result: LearnedRuleDialogResult = {
      categoryId: this.categoryControl.value,
      normalizedVendor: this.vendorControl.value.trim(),
      pattern: this.patternControl.value.trim(),
    };
    this.dialogRef.close(result);
  }
}
