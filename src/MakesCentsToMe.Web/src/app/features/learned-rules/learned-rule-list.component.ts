import { DatePipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { forkJoin } from 'rxjs';
import { ApiService, Category, LearnedRule } from '../../services/api.service';
import {
  LearnedRuleDialogComponent,
  LearnedRuleDialogData,
  LearnedRuleDialogResult,
} from './learned-rule-dialog.component';

@Component({
  selector: 'app-learned-rule-list',
  standalone: true,
  imports: [
    DatePipe,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTableModule,
    MatToolbarModule,
    MatTooltipModule,
  ],
  template: `
    <mat-toolbar>
      <span>Learned Rules</span>
      <span class="spacer"></span>
      <button mat-flat-button (click)="openAddDialog()" [disabled]="isLoading()">
        <mat-icon>add</mat-icon>
        Add Rule
      </button>
    </mat-toolbar>

    @if (isLoading()) {
      <div class="loading-container">
        <mat-spinner diameter="48"></mat-spinner>
      </div>
    } @else if (errorMessage()) {
      <div class="error-container">
        <p>{{ errorMessage() }}</p>
        <button mat-button (click)="loadData()">Retry</button>
      </div>
    } @else {
      <table mat-table [dataSource]="rules()" class="full-width">
        <ng-container matColumnDef="pattern">
          <th mat-header-cell *matHeaderCellDef>Pattern</th>
          <td mat-cell *matCellDef="let row">{{ row.pattern }}</td>
        </ng-container>

        <ng-container matColumnDef="vendor">
          <th mat-header-cell *matHeaderCellDef>Vendor</th>
          <td mat-cell *matCellDef="let row">{{ row.normalizedVendor }}</td>
        </ng-container>

        <ng-container matColumnDef="category">
          <th mat-header-cell *matHeaderCellDef>Category</th>
          <td mat-cell *matCellDef="let row">{{ row.categoryName }}</td>
        </ng-container>

        <ng-container matColumnDef="updated">
          <th mat-header-cell *matHeaderCellDef>Updated</th>
          <td mat-cell *matCellDef="let row">{{ row.updatedAt | date: 'mediumDate' }}</td>
        </ng-container>

        <ng-container matColumnDef="actions">
          <th mat-header-cell *matHeaderCellDef>Actions</th>
          <td mat-cell *matCellDef="let row">
            <button mat-icon-button matTooltip="Edit" (click)="openEditDialog(row)">
              <mat-icon>edit</mat-icon>
            </button>
            <button mat-icon-button matTooltip="Delete" color="warn" (click)="deleteRule(row)">
              <mat-icon>delete</mat-icon>
            </button>
          </td>
        </ng-container>

        <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
        <tr mat-row *matRowDef="let row; columns: displayedColumns"></tr>

        <tr class="mat-row" *matNoDataRow>
          <td class="mat-cell no-data-cell" [attr.colspan]="displayedColumns.length">
            No learned rules yet. Accept or override a transaction in the review queue to create one.
          </td>
        </tr>
      </table>
    }
  `,
  styles: [
    `
      .error-container {
        padding: 24px;
        text-align: center;
      }
      .full-width {
        width: 100%;
      }
      .loading-container {
        display: flex;
        justify-content: center;
        padding: 48px;
      }
      .no-data-cell {
        padding: 24px;
        text-align: center;
      }
      .spacer {
        flex: 1 1 auto;
      }
    `,
  ],
})
export class LearnedRuleListComponent implements OnInit {
  protected readonly categories = signal<Category[]>([]);
  protected readonly displayedColumns = ['pattern', 'vendor', 'category', 'updated', 'actions'];
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly isLoading = signal(false);
  protected readonly rules = signal<LearnedRule[]>([]);

  private readonly apiService = inject(ApiService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  deleteRule(rule: LearnedRule): void {
    if (!confirm(`Delete the rule for "${rule.pattern}"? This cannot be undone.`)) return;
    this.apiService.deleteLearnedRule(rule.id).subscribe({
      next: () => {
        this.rules.update((list) => list.filter((item) => item.id !== rule.id));
        this.snackBar.open(`Rule "${rule.pattern}" deleted.`, 'Dismiss', { duration: 3000 });
      },
      error: () => {
        this.snackBar.open('Failed to delete rule.', 'Dismiss', { duration: 4000 });
      },
    });
  }

  loadData(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    forkJoin({
      categories: this.apiService.getCategories(),
      rules: this.apiService.getLearnedRules(),
    }).subscribe({
      next: ({ categories, rules }) => {
        this.categories.set(categories);
        this.rules.set(rules);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('Failed to load learned rules. Is the API running?');
        this.isLoading.set(false);
      },
    });
  }

  ngOnInit(): void {
    this.loadData();
  }

  openAddDialog(): void {
    const data: LearnedRuleDialogData = { categories: this.categories(), mode: 'create' };
    const reference = this.dialog.open(LearnedRuleDialogComponent, { data, width: '440px' });
    reference.afterClosed().subscribe((result: LearnedRuleDialogResult | undefined) => {
      if (!result) return;
      this.apiService.createLearnedRule({ ...result, sourceTransactionId: null }).subscribe({
        next: () => {
          this.snackBar.open(`Rule "${result.pattern}" added.`, 'Dismiss', { duration: 3000 });
          this.reloadRules();
        },
        error: () => {
          this.snackBar.open('Failed to add rule.', 'Dismiss', { duration: 4000 });
        },
      });
    });
  }

  openEditDialog(rule: LearnedRule): void {
    const data: LearnedRuleDialogData = { categories: this.categories(), mode: 'edit', rule };
    const reference = this.dialog.open(LearnedRuleDialogComponent, { data, width: '440px' });
    reference.afterClosed().subscribe((result: LearnedRuleDialogResult | undefined) => {
      if (!result) return;
      this.apiService.updateLearnedRule(rule.id, result).subscribe({
        next: () => {
          this.snackBar.open(`Rule "${result.pattern}" updated.`, 'Dismiss', { duration: 3000 });
          this.reloadRules();
        },
        error: () => {
          this.snackBar.open('Failed to update rule.', 'Dismiss', { duration: 4000 });
        },
      });
    });
  }

  private reloadRules(): void {
    this.apiService.getLearnedRules().subscribe({
      next: (rules) => this.rules.set(rules),
    });
  }
}
