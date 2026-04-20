import { Routes } from '@angular/router';
import { adminGuard } from './core/admin.guard';
import { authGuard } from './core/auth.guard';
import { AdminDashboardComponent } from './pages/admin/admin-dashboard.component';
import { AuthPageComponent } from './pages/auth-page.component';
import { HelpdeskPageComponent } from './pages/helpdesk-page.component';

export const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: 'login', component: AuthPageComponent },
  { path: 'helpdesk', component: HelpdeskPageComponent, canActivate: [authGuard] },
  { path: 'admin', component: AdminDashboardComponent, canActivate: [adminGuard] },
  { path: '**', redirectTo: 'login' }
];
