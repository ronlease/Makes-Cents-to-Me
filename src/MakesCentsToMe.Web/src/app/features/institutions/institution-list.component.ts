import { Component, inject, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ApiService, Institution } from '../../services/api.service';
import {
  InstitutionDialogComponent,
  InstitutionDialogData,
  InstitutionDialogResult,
} from './institution-dialog.component';

@Component({
  selector: 'app-institution-list',
  standalone: true,
  imports: [
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTableModule,
    MatToolbarModule,
    MatTooltipModule,
  ],
  template: `
    <mat-toolbar>
      <span>Institutions</span>
      <span class="spacer"></span>
      <button mat-flat-button (click)="openAddDialog()">
        <mat-icon>add</mat-icon>
        Add Institution
      </button>
    </mat-toolbar>

    @if (isLoading()) {
      <div class="loading-container">
        <mat-spinner diameter="48"></mat-spinner>
      </div>
    } @else if (errorMessage()) {
      <div class="error-container">
        <p>{{ errorMessage() }}</p>
        <button mat-button (click)="loadInstitutions()">Retry</button>
      </div>
    } @else {
      <table mat-table [dataSource]="institutions()" class="full-width">

        <ng-container matColumnDef="name">
          <th mat-header-cell *matHeaderCellDef>Name</th>
          <td mat-cell *matCellDef="let row">{{ row.name }}</td>
        </ng-container>

        <ng-container matColumnDef="accountCount">
          <th mat-header-cell *matHeaderCellDef># Accounts</th>
          <td mat-cell *matCellDef="let row">{{ row.accountCount }}</td>
        </ng-container>

        <ng-container matColumnDef="actions">
          <th mat-header-cell *matHeaderCellDef>Actions</th>
          <td mat-cell *matCellDef="let row">
            <button mat-icon-button matTooltip="View Accounts" (click)="viewAccounts(row)">
              <mat-icon>account_balance</mat-icon>
            </button>
            <button mat-icon-button matTooltip="Edit" (click)="openEditDialog(row)">
              <mat-icon>edit</mat-icon>
            </button>
            <button mat-icon-button matTooltip="Delete" color="warn" (click)="deleteInstitution(row)">
              <mat-icon>delete</mat-icon>
            </button>
          </td>
        </ng-container>

        <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
        <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>

        <tr class="mat-row" *matNoDataRow>
          <td class="mat-cell no-data-cell" [attr.colspan]="displayedColumns.length">
            No institutions found. Add one to get started.
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
export class InstitutionListComponent implements OnInit {
  protected readonly displayedColumns = ['name', 'accountCount', 'actions'];
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly institutions = signal<Institution[]>([]);
  protected readonly isLoading = signal(false);

  private readonly apiService = inject(ApiService);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);

  deleteInstitution(institution: Institution): void {
    if (!confirm(`Delete "${institution.name}"? This cannot be undone.`)) return;
    this.apiService.deleteInstitution(institution.institutionId).subscribe({
      next: () => {
        this.institutions.update(list => list.filter(i => i.institutionId !== institution.institutionId));
        this.snackBar.open(`"${institution.name}" deleted.`, 'Dismiss', { duration: 3000 });
      },
      error: () => {
        this.snackBar.open('Failed to delete institution.', 'Dismiss', { duration: 4000 });
      },
    });
  }

  loadInstitutions(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.apiService.getInstitutions().subscribe({
      next: data => {
        this.institutions.set(data);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('Failed to load institutions. Is the API running?');
        this.isLoading.set(false);
      },
    });
  }

  ngOnInit(): void {
    this.loadInstitutions();
  }

  openAddDialog(): void {
    const data: InstitutionDialogData = {};
    const ref = this.dialog.open(InstitutionDialogComponent, { data, width: '400px' });
    ref.afterClosed().subscribe((result: InstitutionDialogResult | undefined) => {
      if (!result) return;
      this.apiService.createInstitution({ name: result.name }).subscribe({
        next: created => {
          this.institutions.update(list => [...list, created]);
          this.snackBar.open(`"${created.name}" added.`, 'Dismiss', { duration: 3000 });
        },
        error: () => {
          this.snackBar.open('Failed to add institution.', 'Dismiss', { duration: 4000 });
        },
      });
    });
  }

  openEditDialog(institution: Institution): void {
    const data: InstitutionDialogData = { institution };
    const ref = this.dialog.open(InstitutionDialogComponent, { data, width: '400px' });
    ref.afterClosed().subscribe((result: InstitutionDialogResult | undefined) => {
      if (!result) return;
      this.apiService.updateInstitution(institution.institutionId, { name: result.name }).subscribe({
        next: updated => {
          this.institutions.update(list =>
            list.map(i => (i.institutionId === updated.institutionId ? updated : i))
          );
          this.snackBar.open(`"${updated.name}" updated.`, 'Dismiss', { duration: 3000 });
        },
        error: () => {
          this.snackBar.open('Failed to update institution.', 'Dismiss', { duration: 4000 });
        },
      });
    });
  }

  viewAccounts(institution: Institution): void {
    this.router.navigate(['/institutions', institution.institutionId, 'accounts']);
  }
}
