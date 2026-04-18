import { CurrencyPipe, DatePipe, NgClass, PercentPipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { MatBadgeModule } from '@angular/material/badge';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ApiService, Category, ReviewTransaction } from '../../services/api.service';
import {
  ReviewOverrideDialogComponent,
  ReviewOverrideDialogData,
  ReviewOverrideDialogResult,
} from './review-override-dialog.component';

@Component({
  selector: 'app-review-queue',
  standalone: true,
  imports: [
    CurrencyPipe,
    DatePipe,
    MatBadgeModule,
    MatButtonModule,
    MatChipsModule,
    MatIconModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatTableModule,
    MatToolbarModule,
    MatTooltipModule,
    NgClass,
    PercentPipe,
  ],
  template: `
    <mat-toolbar>
      <span>Review Queue</span>
      @if (pendingTransactions().length > 0) {
        <span class="pending-badge">{{ pendingTransactions().length }} pending</span>
      }
      <span class="spacer"></span>
      <button
        mat-flat-button
        color="primary"
        [disabled]="isAcceptingAll() || pendingTransactions().length === 0"
        (click)="acceptAll()">
        @if (isAcceptingAll()) {
          <mat-spinner diameter="20" class="inline-spinner"></mat-spinner>
        } @else {
          <mat-icon>done_all</mat-icon>
        }
        Accept All High Confidence
      </button>
    </mat-toolbar>

    <div class="summary-bar">
      <span>Total: {{ allTransactions().length }}</span>
      <span class="summary-divider">|</span>
      <span class="summary-pending">Pending: {{ pendingCount() }}</span>
      <span class="summary-divider">|</span>
      <span class="summary-pending-analysis">Awaiting Analysis: {{ pendingAnalysisCount() }}</span>
      <span class="summary-divider">|</span>
      <span class="summary-accepted">Accepted: {{ acceptedCount() }}</span>
      <span class="summary-divider">|</span>
      <span class="summary-overridden">Overridden: {{ overriddenCount() }}</span>
    </div>

    @if (isLoading()) {
      <div class="loading-container">
        <mat-spinner diameter="48"></mat-spinner>
      </div>
    } @else if (errorMessage()) {
      <div class="error-container">
        <p>{{ errorMessage() }}</p>
        <button mat-button (click)="loadReviewQueue()">Retry</button>
      </div>
    } @else {
      <table mat-table [dataSource]="pagedTransactions()" class="full-width">

        <ng-container matColumnDef="date">
          <th mat-header-cell *matHeaderCellDef>Date</th>
          <td mat-cell *matCellDef="let row" [ngClass]="rowClass(row)">
            {{ row.date | date:'mediumDate' }}
          </td>
        </ng-container>

        <ng-container matColumnDef="description">
          <th mat-header-cell *matHeaderCellDef>Raw Description</th>
          <td mat-cell *matCellDef="let row" [ngClass]="rowClass(row)">
            <span class="description-text" [matTooltip]="row.description">{{ row.description }}</span>
          </td>
        </ng-container>

        <ng-container matColumnDef="suggestedNormalizedVendor">
          <th mat-header-cell *matHeaderCellDef>Suggested Vendor</th>
          <td mat-cell *matCellDef="let row" [ngClass]="rowClass(row)">
            {{ row.suggestedNormalizedVendor ?? '—' }}
          </td>
        </ng-container>

        <ng-container matColumnDef="suggestedCategory">
          <th mat-header-cell *matHeaderCellDef>Suggested Category</th>
          <td mat-cell *matCellDef="let row" [ngClass]="rowClass(row)">
            {{ row.suggestedCategory ?? '—' }}
          </td>
        </ng-container>

        <ng-container matColumnDef="confidence">
          <th mat-header-cell *matHeaderCellDef>Confidence</th>
          <td mat-cell *matCellDef="let row" [ngClass]="rowClass(row)">
            @if (row.confidence !== null && row.confidence !== undefined) {
              <span class="confidence-badge" [ngClass]="confidenceClass(row.confidence)">
                {{ row.confidence | percent:'1.0-0' }}
              </span>
            } @else {
              <span class="confidence-badge confidence-none">—</span>
            }
          </td>
        </ng-container>

        <ng-container matColumnDef="amount">
          <th mat-header-cell *matHeaderCellDef>Amount</th>
          <td mat-cell *matCellDef="let row" [ngClass]="rowClass(row)">
            <span [ngClass]="row.amount < 0 ? 'amount-credit' : 'amount-debit'">
              {{ row.amount | currency }}
            </span>
          </td>
        </ng-container>

        <ng-container matColumnDef="accountName">
          <th mat-header-cell *matHeaderCellDef>Account</th>
          <td mat-cell *matCellDef="let row" [ngClass]="rowClass(row)">
            {{ row.accountName }}
          </td>
        </ng-container>

        <ng-container matColumnDef="status">
          <th mat-header-cell *matHeaderCellDef>Status</th>
          <td mat-cell *matCellDef="let row" [ngClass]="rowClass(row)">
            <span class="status-badge" [ngClass]="statusClass(row.status)">
              {{ statusLabel(row.status) }}
            </span>
          </td>
        </ng-container>

        <ng-container matColumnDef="actions">
          <th mat-header-cell *matHeaderCellDef>Actions</th>
          <td mat-cell *matCellDef="let row" [ngClass]="rowClass(row)">
            @if (isPending(row)) {
              <button
                mat-icon-button
                color="primary"
                matTooltip="Accept suggestion"
                [disabled]="isRowBusy(row.id)"
                (click)="acceptTransaction(row)">
                <mat-icon>check_circle</mat-icon>
              </button>
              <button
                mat-icon-button
                matTooltip="Override vendor or category"
                [disabled]="isRowBusy(row.id)"
                (click)="openOverrideDialog(row)">
                <mat-icon>edit</mat-icon>
              </button>
            } @else {
              <span class="completed-label">{{ completedLabel(row.status) }}</span>
            }
          </td>
        </ng-container>

        <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
        <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>

        <tr class="mat-row" *matNoDataRow>
          <td class="mat-cell no-data-cell" [attr.colspan]="displayedColumns.length">
            No transactions in the review queue.
          </td>
        </tr>
      </table>

      <mat-paginator
        [length]="allTransactions().length"
        [pageSize]="pageSize"
        [pageSizeOptions]="pageSizeOptions"
        [pageIndex]="currentPageIndex()"
        (page)="onPageChange($event)"
        showFirstLastButtons>
      </mat-paginator>
    }
  `,
  styles: [`
    .amount-credit { color: #2e7d32; }
    .amount-debit { color: inherit; }
    .completed-label { color: var(--mat-sys-on-surface-variant); font-size: 0.8rem; padding: 0 8px; }

    .confidence-badge {
      border-radius: 12px;
      display: inline-block;
      font-size: 0.75rem;
      font-weight: 600;
      padding: 2px 10px;
    }
    .confidence-high { background-color: #c8e6c9; color: #1b5e20; }
    .confidence-medium { background-color: #fff9c4; color: #f57f17; }
    .confidence-low { background-color: #ffcdd2; color: #b71c1c; }
    .confidence-none { background-color: var(--mat-sys-surface-variant); color: var(--mat-sys-on-surface-variant); }

    .description-text {
      display: inline-block;
      max-width: 280px;
      overflow: hidden;
      text-overflow: ellipsis;
      vertical-align: middle;
      white-space: nowrap;
    }

    .error-container { padding: 24px; text-align: center; }

    .full-width { width: 100%; }

    .inline-spinner { display: inline-block; margin-right: 8px; vertical-align: middle; }

    .loading-container { display: flex; justify-content: center; padding: 48px; }

    .no-data-cell { padding: 24px; text-align: center; }

    .pending-badge {
      background: var(--mat-sys-primary-container);
      border-radius: 12px;
      color: var(--mat-sys-on-primary-container);
      font-size: 0.8rem;
      margin-left: 12px;
      padding: 2px 10px;
    }

    .row-completed { opacity: 0.55; }

    .spacer { flex: 1 1 auto; }

    .status-badge {
      border-radius: 12px;
      display: inline-block;
      font-size: 0.75rem;
      font-weight: 600;
      padding: 2px 10px;
    }
    .status-pending { background-color: #e3f2fd; color: #0d47a1; }
    .status-pending-analysis { background-color: #f3e5f5; color: #4a148c; }
    .status-accepted { background-color: #c8e6c9; color: #1b5e20; }
    .status-overridden { background-color: #fff9c4; color: #f57f17; }
    .status-other { background-color: var(--mat-sys-surface-variant); color: var(--mat-sys-on-surface-variant); }

    .summary-accepted { color: #2e7d32; }
    .summary-bar {
      align-items: center;
      background: var(--mat-sys-surface-variant);
      display: flex;
      font-size: 0.85rem;
      gap: 12px;
      padding: 8px 16px;
    }
    .summary-divider { color: var(--mat-sys-outline); }
    .summary-overridden { color: #e65100; }
    .summary-pending { color: #0d47a1; }
    .summary-pending-analysis { color: #4a148c; }
  `],
})
export class ReviewQueueComponent implements OnInit {
  protected readonly currentPageIndex = signal(0);
  protected readonly displayedColumns = [
    'date',
    'description',
    'suggestedNormalizedVendor',
    'suggestedCategory',
    'confidence',
    'amount',
    'accountName',
    'status',
    'actions',
  ];
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly isAcceptingAll = signal(false);
  protected readonly isLoading = signal(false);
  protected readonly allTransactions = signal<ReviewTransaction[]>([]);
  protected readonly pageSizeOptions = [10, 25, 50];
  protected readonly pageSize = 25;

  private readonly apiService = inject(ApiService);
  private readonly busyRowIds = signal<Set<string>>(new Set());
  private readonly categories = signal<Category[]>([]);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  protected acceptedCount(): number {
    return this.allTransactions().filter(t => t.status === 'Accepted').length;
  }

  acceptAll(): void {
    this.isAcceptingAll.set(true);
    this.apiService.acceptAllTransactions().subscribe({
      next: count => {
        this.isAcceptingAll.set(false);
        this.snackBar.open(`Accepted ${count} transaction(s).`, 'Dismiss', { duration: 3000 });
        this.loadReviewQueue();
      },
      error: () => {
        this.isAcceptingAll.set(false);
        this.snackBar.open('Failed to accept all transactions.', 'Dismiss', { duration: 4000 });
      },
    });
  }

  acceptTransaction(transaction: ReviewTransaction): void {
    this.markRowBusy(transaction.id, true);
    this.apiService.acceptTransaction(transaction.id).subscribe({
      next: updated => {
        this.markRowBusy(transaction.id, false);
        this.allTransactions.update(list =>
          list.map(t => (t.id === updated.id ? updated : t))
        );
        this.snackBar.open('Transaction accepted.', 'Dismiss', { duration: 2500 });
      },
      error: () => {
        this.markRowBusy(transaction.id, false);
        this.snackBar.open('Failed to accept transaction.', 'Dismiss', { duration: 4000 });
      },
    });
  }

  protected completedLabel(status: string): string {
    if (status === 'Accepted') return 'Accepted';
    if (status === 'Overridden') return 'Overridden';
    return status;
  }

  protected confidenceClass(confidence: number): string {
    if (confidence >= 0.8) return 'confidence-high';
    if (confidence >= 0.5) return 'confidence-medium';
    return 'confidence-low';
  }

  protected isRowBusy(transactionId: string): boolean {
    return this.busyRowIds().has(transactionId);
  }

  protected isPending(transaction: ReviewTransaction): boolean {
    return transaction.status === 'PendingReview' || transaction.status === 'PendingAnalysis';
  }

  loadReviewQueue(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.currentPageIndex.set(0);
    this.apiService.getReviewQueue().subscribe({
      next: data => {
        this.allTransactions.set(data);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('Failed to load review queue. Is the API running?');
        this.isLoading.set(false);
      },
    });
  }

  ngOnInit(): void {
    this.loadReviewQueue();
    this.apiService.getCategories().subscribe({
      next: data => this.categories.set(data),
    });
  }

  protected onPageChange(event: PageEvent): void {
    this.currentPageIndex.set(event.pageIndex);
  }

  openOverrideDialog(transaction: ReviewTransaction): void {
    const data: ReviewOverrideDialogData = {
      categories: this.categories(),
      transaction,
    };
    const ref = this.dialog.open(ReviewOverrideDialogComponent, { data, width: '480px' });
    ref.afterClosed().subscribe((result: ReviewOverrideDialogResult | undefined) => {
      if (!result) return;
      this.markRowBusy(transaction.id, true);
      this.apiService.overrideTransaction(transaction.id, result).subscribe({
        next: updated => {
          this.markRowBusy(transaction.id, false);
          this.allTransactions.update(list =>
            list.map(t => (t.id === updated.id ? updated : t))
          );
          this.snackBar.open('Transaction overridden.', 'Dismiss', { duration: 2500 });
        },
        error: () => {
          this.markRowBusy(transaction.id, false);
          this.snackBar.open('Failed to override transaction.', 'Dismiss', { duration: 4000 });
        },
      });
    });
  }

  protected pagedTransactions(): ReviewTransaction[] {
    const start = this.currentPageIndex() * this.pageSize;
    return this.allTransactions().slice(start, start + this.pageSize);
  }

  protected pendingAnalysisCount(): number {
    return this.allTransactions().filter(t => t.status === 'PendingAnalysis').length;
  }

  protected pendingCount(): number {
    return this.allTransactions().filter(t => t.status === 'PendingReview').length;
  }

  protected pendingTransactions(): ReviewTransaction[] {
    return this.allTransactions().filter(t => this.isPending(t));
  }

  protected overriddenCount(): number {
    return this.allTransactions().filter(t => t.status === 'Overridden').length;
  }

  protected rowClass(transaction: ReviewTransaction): string {
    return this.isPending(transaction) ? '' : 'row-completed';
  }

  protected statusClass(status: string): string {
    if (status === 'PendingReview') return 'status-pending';
    if (status === 'PendingAnalysis') return 'status-pending-analysis';
    if (status === 'Accepted') return 'status-accepted';
    if (status === 'Overridden') return 'status-overridden';
    return 'status-other';
  }

  protected statusLabel(status: string): string {
    if (status === 'PendingReview') return 'Pending Review';
    if (status === 'PendingAnalysis') return 'Pending Analysis';
    if (status === 'Accepted') return 'Accepted';
    if (status === 'Overridden') return 'Overridden';
    return status;
  }

  private markRowBusy(transactionId: string, busy: boolean): void {
    this.busyRowIds.update(set => {
      const next = new Set(set);
      if (busy) {
        next.add(transactionId);
      } else {
        next.delete(transactionId);
      }
      return next;
    });
  }
}
