import { Component, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { Account, AccountType } from '../../services/api.service';

export interface AccountDialogData {
  account?: Account;
}

export interface AccountDialogResult {
  accountType: AccountType;
  name: string;
}

@Component({
  selector: 'app-account-dialog',
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
    <h2 mat-dialog-title>{{ data.account ? 'Edit Account' : 'Add Account' }}</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="form-grid">
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Name</mat-label>
          <input matInput formControlName="name" placeholder="e.g. Primary Checking" />
          @if (form.controls.name.hasError('required')) {
            <mat-error>Name is required.</mat-error>
          }
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Account Type</mat-label>
          <mat-select formControlName="accountType">
            @for (option of accountTypeOptions; track option.value) {
              <mat-option [value]="option.value">{{ option.label }}</mat-option>
            }
          </mat-select>
          @if (form.controls.accountType.hasError('required')) {
            <mat-error>Account type is required.</mat-error>
          }
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="cancel()">Cancel</button>
      <button mat-flat-button (click)="save()" [disabled]="form.invalid">Save</button>
    </mat-dialog-actions>
  `,
  styles: [`
    .full-width { width: 100%; min-width: 360px; }
    .form-grid { display: flex; flex-direction: column; gap: 4px; padding-top: 8px; }
    mat-dialog-content { padding-top: 8px; }
  `],
})
export class AccountDialogComponent {
  protected readonly accountTypeOptions: { label: string; value: AccountType }[] = [
    { label: 'Checking', value: 'Checking' },
    { label: 'Credit Card', value: 'CreditCard' },
    { label: 'Money Market', value: 'MoneyMarket' },
    { label: 'Savings', value: 'Savings' },
  ];

  protected readonly data = inject<AccountDialogData>(MAT_DIALOG_DATA);

  protected readonly form = new FormGroup({
    accountType: new FormControl<AccountType>(
      this.data.account?.accountType ?? 'Checking',
      { nonNullable: true, validators: [Validators.required] }
    ),
    name: new FormControl<string>(
      this.data.account?.name ?? '',
      { nonNullable: true, validators: [Validators.required] }
    ),
  });

  private readonly dialogRef = inject(MatDialogRef<AccountDialogComponent>);

  cancel(): void {
    this.dialogRef.close();
  }

  save(): void {
    if (this.form.invalid) return;
    const result: AccountDialogResult = {
      accountType: this.form.controls.accountType.value,
      name: this.form.controls.name.value,
    };
    this.dialogRef.close(result);
  }
}
