import { Component, ViewChild } from '@angular/core';
import { Transaction } from './models/models';
import { TransactionModalComponent } from './components/transaction-modal/transaction-modal.component';
import { AuthService } from './services/auth.service';
import { FinisApiService } from './services/finis-api.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent {
  title = 'finis';

  @ViewChild(TransactionModalComponent) transactionModal?: TransactionModalComponent;

  isTransactionModalOpen = false;
  isBalanceModalOpen = false;
  isResetModalOpen = false;
  transactionToEdit: Transaction | null = null;
  initialDate: string | null = null;

  constructor(
    public authService: AuthService,
    private api: FinisApiService
  ) {}

  openNewTransaction(options?: { day?: number; date?: string }): void {
    this.transactionToEdit = null;
    this.initialDate = options?.date || null;
    this.isTransactionModalOpen = true;
    if (this.transactionModal) {
      this.transactionModal.resetForm(this.initialDate || undefined, null);
    }
  }

  openEditTransaction(transaction: Transaction): void {
    this.transactionToEdit = transaction;
    this.initialDate = transaction.date;
    this.isTransactionModalOpen = true;
    if (this.transactionModal) {
      this.transactionModal.resetForm(undefined, transaction);
    }
  }

  closeTransactionModal(): void {
    this.isTransactionModalOpen = false;
    this.transactionToEdit = null;
  }

  openBalanceModal(): void {
    this.isBalanceModalOpen = true;
  }

  closeBalanceModal(): void {
    this.isBalanceModalOpen = false;
  }

  openResetModal(): void {
    this.isResetModalOpen = true;
  }

  closeResetModal(): void {
    this.isResetModalOpen = false;
  }

  onResetCompleted(): void {
    this.api.notifyDataChanged();
    this.authService.getProfile().subscribe();
  }

  onTransactionSaved(): void {
    // Notify all active views (Dashboard, MonthlyFlow, Economy) to auto-reload
    this.api.notifyDataChanged();
    // Refresh user profile to update initial balance in top navbar
    this.authService.getProfile().subscribe();
  }

  onBalanceUpdated(): void {
    // Notify all active views to recompute carry-over and daily balances
    this.api.notifyDataChanged();
    this.authService.getProfile().subscribe();
  }

  onRouterActivate(componentRef: any): void {
    if (componentRef.openNewTransaction) {
      componentRef.openNewTransaction.subscribe((opts?: any) => this.openNewTransaction(opts));
    }
    if (componentRef.editTransaction) {
      componentRef.editTransaction.subscribe((t: Transaction) => this.openEditTransaction(t));
    }
  }
}
