import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Account, ApiService } from '../../services/api.service';
import {
  AccountDialogComponent,
  AccountDialogData,
  AccountDialogResult,
} from './account-dialog.component';

@Component({
  selector: 'app-account-list',
  standalone: true,
  imports: [
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTableModule,
    MatToolbarModule,
    MatTooltipModule,
    RouterModule,
  ],
  template: `
    <mat-toolbar>
      <button mat-icon-button matTooltip="Back to Institutions" routerLink="/institutions">
        <mat-icon>arrow_back</mat-icon>
      </button>
      <span>Accounts</span>
      <span class="spacer"></span>
      <button mat-flat-button (click)="openAddDialog()">
        <mat-icon>add</mat-icon>
        Add Account
      </button>
    </mat-toolbar>

    @if (isLoading()) {
      <div class="loading-container">
        <mat-spinner diameter="48"></mat-spinner>
      </div>
    } @else if (errorMessage()) {
      <div class="error-container">
        <p>{{ errorMessage() }}</p>
        <button mat-button (click)="loadAccounts()">Retry</button>
      </div>
    } @else {
      <table mat-table [dataSource]="accounts()" class="full-width">

        <ng-container matColumnDef="name">
          <th mat-header-cell *matHeaderCellDef>Name</th>
          <td mat-cell *matCellDef="let row">{{ row.name }}</td>
        </ng-container>

        <ng-container matColumnDef="accountType">
          <th mat-header-cell *matHeaderCellDef>Type</th>
          <td mat-cell *matCellDef="let row">{{ formatAccountType(row.accountType) }}</td>
        </ng-container>

        <ng-container matColumnDef="hasImportProfile">
          <th mat-header-cell *matHeaderCellDef>Has Import Profile</th>
          <td mat-cell *matCellDef="let row">
            <mat-icon [style.color]="row.hasImportProfile ? 'green' : 'gray'">
              {{ row.hasImportProfile ? 'check_circle' : 'radio_button_unchecked' }}
            </mat-icon>
          </td>
        </ng-container>

        <ng-container matColumnDef="actions">
          <th mat-header-cell *matHeaderCellDef>Actions</th>
          <td mat-cell *matCellDef="let row">
            <button mat-icon-button matTooltip="Import Transactions" (click)="navigateToImport(row)">
              <mat-icon>upload_file</mat-icon>
            </button>
            <button mat-icon-button matTooltip="Edit" (click)="openEditDialog(row)">
              <mat-icon>edit</mat-icon>
            </button>
            <button mat-icon-button matTooltip="Delete" color="warn" (click)="deleteAccount(row)">
              <mat-icon>delete</mat-icon>
            </button>
          </td>
        </ng-container>

        <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
        <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>

        <tr class="mat-row" *matNoDataRow>
          <td class="mat-cell no-data-cell" [attr.colspan]="displayedColumns.length">
            No accounts found. Add one to get started.
          </td>
        </tr>
      </table>
    }
  `,
  styles: [`
    .spacer { flex: 1 1 auto; }
    .full-width { width: 100%; }
    .loading-container {
      display: flex;
      justify-content: center;
      padding: 48px;
    }
    .error-container {
      padding: 24px;
      text-align: center;
    }
    .no-data-cell {
      padding: 24px;
      text-align: center;
    }
  `],
})
export class AccountListComponent implements OnInit {
  protected readonly accounts = signal<Account[]>([]);
  protected readonly displayedColumns = ['name', 'accountType', 'hasImportProfile', 'actions'];
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly institutionId = signal<number>(0);
  protected readonly isLoading = signal(false);

  private readonly apiService = inject(ApiService);
  private readonly dialog = inject(MatDialog);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);

  deleteAccount(account: Account): void {
    if (!confirm(`Delete "${account.name}"? This cannot be undone.`)) return;
    this.apiService.deleteAccount(this.institutionId(), account.accountId).subscribe({
      next: () => {
        this.accounts.update(list => list.filter(a => a.accountId !== account.accountId));
        this.snackBar.open(`"${account.name}" deleted.`, 'Dismiss', { duration: 3000 });
      },
      error: () => {
        this.snackBar.open('Failed to delete account.', 'Dismiss', { duration: 4000 });
      },
    });
  }

  formatAccountType(accountType: string): string {
    const map: Record<string, string> = {
      Checking: 'Checking',
      CreditCard: 'Credit Card',
      MoneyMarket: 'Money Market',
      Savings: 'Savings',
    };
    return map[accountType] ?? accountType;
  }

  loadAccounts(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.apiService.getAccounts(this.institutionId()).subscribe({
      next: data => {
        this.accounts.set(data);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('Failed to load accounts. Is the API running?');
        this.isLoading.set(false);
      },
    });
  }

  navigateToImport(account: Account): void {
    this.router.navigate(['/accounts', account.accountId, 'import']);
  }

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('institutionId');
    this.institutionId.set(idParam ? Number(idParam) : 0);
    this.loadAccounts();
  }

  openAddDialog(): void {
    const data: AccountDialogData = {};
    const ref = this.dialog.open(AccountDialogComponent, { data, width: '420px' });
    ref.afterClosed().subscribe((result: AccountDialogResult | undefined) => {
      if (!result) return;
      this.apiService.createAccount(this.institutionId(), result).subscribe({
        next: created => {
          this.accounts.update(list => [...list, created]);
          this.snackBar.open(`"${created.name}" added.`, 'Dismiss', { duration: 3000 });
        },
        error: () => {
          this.snackBar.open('Failed to add account.', 'Dismiss', { duration: 4000 });
        },
      });
    });
  }

  openEditDialog(account: Account): void {
    const data: AccountDialogData = { account };
    const ref = this.dialog.open(AccountDialogComponent, { data, width: '420px' });
    ref.afterClosed().subscribe((result: AccountDialogResult | undefined) => {
      if (!result) return;
      this.apiService.updateAccount(this.institutionId(), account.accountId, result).subscribe({
        next: updated => {
          this.accounts.update(list =>
            list.map(a => (a.accountId === updated.accountId ? updated : a))
          );
          this.snackBar.open(`"${updated.name}" updated.`, 'Dismiss', { duration: 3000 });
        },
        error: () => {
          this.snackBar.open('Failed to update account.', 'Dismiss', { duration: 4000 });
        },
      });
    });
  }
}
