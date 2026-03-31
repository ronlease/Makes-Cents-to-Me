import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'institutions',
    pathMatch: 'full',
  },
  {
    path: 'institutions',
    loadComponent: () =>
      import('./features/institutions/institution-list.component').then(
        m => m.InstitutionListComponent
      ),
  },
  {
    path: 'institutions/:institutionId/accounts',
    loadComponent: () =>
      import('./features/accounts/account-list.component').then(
        m => m.AccountListComponent
      ),
  },
  {
    path: 'categories',
    loadComponent: () =>
      import('./features/categories/category-list.component').then(
        m => m.CategoryListComponent
      ),
  },
  {
    path: 'accounts/:accountId/import',
    loadComponent: () =>
      import('./features/import/import.component').then(
        m => m.ImportComponent
      ),
  },
  {
    path: 'review',
    loadComponent: () =>
      import('./features/review/review-queue.component').then(
        m => m.ReviewQueueComponent
      ),
  },
];
