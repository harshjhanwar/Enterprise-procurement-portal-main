import { Routes } from '@angular/router';
import { adminGuard, authGuard } from './core/guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login.component').then(module => module.LoginComponent)
  },
  {
    path: '',
    canActivate: [authGuard],
    children: [
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./features/dashboard/dashboard.component').then(module => module.DashboardComponent)
      },
      {
        path: 'plans',
        loadComponent: () =>
          import('./features/plans/plans.component').then(module => module.PlansComponent)
      },
      {
        path: 'plans/new',
        loadComponent: () =>
          import('./features/plans/plan-editor.component').then(module => module.PlanEditorComponent)
      },
      {
        path: 'plans/:id',
        loadComponent: () =>
          import('./features/plans/plan-editor.component').then(module => module.PlanEditorComponent)
      },
      {
        path: 'admin',
        canActivate: [adminGuard],
        loadComponent: () =>
          import('./features/admin/admin.component').then(module => module.AdminComponent)
      },
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'dashboard'
      }
    ]
  },
  {
    path: '**',
    redirectTo: 'dashboard'
  }
];
