import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { LoginComponent } from './components/login/login.component';
import { RegisterComponent } from './components/register/register.component';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { MonthlyFlowComponent } from './components/monthly-flow/monthly-flow.component';
import { EconomyReportComponent } from './components/economy-report/economy-report.component';
import { CategoriesComponent } from './components/categories/categories.component';
import { CreditCardsComponent } from './components/credit-cards/credit-cards.component';
import { AccountsComponent } from './components/accounts/accounts.component';
import { GoalsDebtsComponent } from './components/goals-debts/goals-debts.component';
import { AuthGuard } from './services/auth.guard';

const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'dashboard', component: DashboardComponent, canActivate: [AuthGuard] },
  { path: 'termometro', component: MonthlyFlowComponent, canActivate: [AuthGuard] },
  { path: 'economia', component: EconomyReportComponent, canActivate: [AuthGuard] },
  { path: 'categorias', component: CategoriesComponent, canActivate: [AuthGuard] },
  { path: 'cartoes', component: CreditCardsComponent, canActivate: [AuthGuard] },
  { path: 'contas', component: AccountsComponent, canActivate: [AuthGuard] },
  { path: 'planejamento', component: GoalsDebtsComponent, canActivate: [AuthGuard] },
  { path: 'metas', redirectTo: '/planejamento' },
  { path: 'dividas', redirectTo: '/planejamento' },
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  { path: '**', redirectTo: '/dashboard' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
