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
    path: 'accounts/:accountId/import',
    loadComponent: () =>
      import('./features/import/import.component').then(
        m => m.ImportComponent
      ),
  },
];
