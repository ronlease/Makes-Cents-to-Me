import { Component, inject, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ApiService, Category } from '../../services/api.service';
import {
  CategoryDialogComponent,
  CategoryDialogData,
  CategoryDialogResult,
} from './category-dialog.component';

@Component({
  selector: 'app-category-list',
  standalone: true,
  imports: [
    MatButtonModule,
    MatChipsModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTableModule,
    MatToolbarModule,
    MatTooltipModule,
  ],
  template: `
    <mat-toolbar>
      <span>Categories</span>
      <span class="spacer"></span>
      <button mat-flat-button (click)="openAddDialog()">
        <mat-icon>add</mat-icon>
        Add Category
      </button>
    </mat-toolbar>

    @if (isLoading()) {
      <div class="loading-container">
        <mat-spinner diameter="48"></mat-spinner>
      </div>
    } @else if (errorMessage()) {
      <div class="error-container">
        <p>{{ errorMessage() }}</p>
        <button mat-button (click)="loadCategories()">Retry</button>
      </div>
    } @else {
      <table mat-table [dataSource]="categories()" class="full-width">

        <ng-container matColumnDef="name">
          <th mat-header-cell *matHeaderCellDef>Name</th>
          <td mat-cell *matCellDef="let row">
            <span>{{ row.name }}</span>
            @if (row.isDefault) {
              <mat-chip class="default-chip" disableRipple>Default</mat-chip>
            }
          </td>
        </ng-container>

        <ng-container matColumnDef="transactionCount">
          <th mat-header-cell *matHeaderCellDef># Transactions</th>
          <td mat-cell *matCellDef="let row">{{ row.transactionCount }}</td>
        </ng-container>

        <ng-container matColumnDef="actions">
          <th mat-header-cell *matHeaderCellDef>Actions</th>
          <td mat-cell *matCellDef="let row">
            <button mat-icon-button matTooltip="Edit" (click)="openEditDialog(row)">
              <mat-icon>edit</mat-icon>
            </button>
            <button
              mat-icon-button
              matTooltip="Delete"
              color="warn"
              [disabled]="row.isDefault"
              [matTooltip]="row.isDefault ? 'Default categories cannot be deleted' : 'Delete'"
              (click)="deleteCategory(row)">
              <mat-icon>delete</mat-icon>
            </button>
          </td>
        </ng-container>

        <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
        <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>

        <tr class="mat-row" *matNoDataRow>
          <td class="mat-cell no-data-cell" [attr.colspan]="displayedColumns.length">
            No categories found. Add one to get started.
          </td>
        </tr>
      </table>
    }
  `,
  styles: [`
    .default-chip {
      margin-left: 8px;
      font-size: 11px;
      height: 20px;
      background-color: var(--mat-sys-primary-container);
      color: var(--mat-sys-on-primary-container);
    }
    .error-container {
      padding: 24px;
      text-align: center;
    }
    .full-width { width: 100%; }
    .loading-container {
      display: flex;
      justify-content: center;
      padding: 48px;
    }
    .no-data-cell {
      padding: 24px;
      text-align: center;
    }
    .spacer { flex: 1 1 auto; }
  `],
})
export class CategoryListComponent implements OnInit {
  protected readonly categories = signal<Category[]>([]);
  protected readonly displayedColumns = ['name', 'transactionCount', 'actions'];
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly isLoading = signal(false);

  private readonly apiService = inject(ApiService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  deleteCategory(category: Category): void {
    if (category.isDefault) return;
    if (!confirm(`Delete "${category.name}"? This cannot be undone.`)) return;
    this.apiService.deleteCategory(category.id).subscribe({
      next: () => {
        this.categories.update(list => list.filter(c => c.id !== category.id));
        this.snackBar.open(`"${category.name}" deleted.`, 'Dismiss', { duration: 3000 });
      },
      error: () => {
        this.snackBar.open('Failed to delete category.', 'Dismiss', { duration: 4000 });
      },
    });
  }

  loadCategories(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.apiService.getCategories().subscribe({
      next: data => {
        this.categories.set(data);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('Failed to load categories. Is the API running?');
        this.isLoading.set(false);
      },
    });
  }

  ngOnInit(): void {
    this.loadCategories();
  }

  openAddDialog(): void {
    const data: CategoryDialogData = {};
    const reference = this.dialog.open(CategoryDialogComponent, { data, width: '400px' });
    reference.afterClosed().subscribe((result: CategoryDialogResult | undefined) => {
      if (!result) return;
      this.apiService.createCategory({ name: result.name }).subscribe({
        next: created => {
          this.categories.update(list => [...list, created]);
          this.snackBar.open(`"${created.name}" added.`, 'Dismiss', { duration: 3000 });
        },
        error: () => {
          this.snackBar.open('Failed to add category.', 'Dismiss', { duration: 4000 });
        },
      });
    });
  }

  openEditDialog(category: Category): void {
    const data: CategoryDialogData = { category };
    const reference = this.dialog.open(CategoryDialogComponent, { data, width: '400px' });
    reference.afterClosed().subscribe((result: CategoryDialogResult | undefined) => {
      if (!result) return;
      this.apiService.updateCategory(category.id, { name: result.name }).subscribe({
        next: updated => {
          this.categories.update(list =>
            list.map(c => (c.id === updated.id ? updated : c))
          );
          this.snackBar.open(`"${updated.name}" updated.`, 'Dismiss', { duration: 3000 });
        },
        error: () => {
          this.snackBar.open('Failed to update category.', 'Dismiss', { duration: 4000 });
        },
      });
    });
  }
}
