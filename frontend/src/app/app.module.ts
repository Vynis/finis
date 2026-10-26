import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { FormsModule } from '@angular/forms';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { NavbarComponent } from './components/navbar/navbar.component';
import { LoginComponent } from './components/login/login.component';
import { RegisterComponent } from './components/register/register.component';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { MonthlyFlowComponent } from './components/monthly-flow/monthly-flow.component';
import { EconomyReportComponent } from './components/economy-report/economy-report.component';
import { TransactionModalComponent } from './components/transaction-modal/transaction-modal.component';
import { InitialBalanceModalComponent } from './components/initial-balance-modal/initial-balance-modal.component';
import { ResetModalComponent } from './components/reset-modal/reset-modal.component';
import { CategoriesComponent } from './components/categories/categories.component';
import { CreditCardsComponent } from './components/credit-cards/credit-cards.component';
import { JwtInterceptor } from './services/jwt.interceptor';

@NgModule({
  declarations: [
    AppComponent,
    NavbarComponent,
    LoginComponent,
    RegisterComponent,
    DashboardComponent,
    MonthlyFlowComponent,
    EconomyReportComponent,
    TransactionModalComponent,
    InitialBalanceModalComponent,
    ResetModalComponent,
    CategoriesComponent,
    CreditCardsComponent
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    FormsModule,
    HttpClientModule
  ],
  providers: [
    { provide: HTTP_INTERCEPTORS, useClass: JwtInterceptor, multi: true }
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
