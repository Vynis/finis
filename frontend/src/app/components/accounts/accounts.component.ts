import { Component, OnDestroy, OnInit } from '@angular/core';
import { Subscription } from 'rxjs';
import { FinisApiService } from '../../services/finis-api.service';
import { Account, AccountCreateUpdate, AccountType } from '../../models/models';

@Component({
  selector: 'app-accounts',
  templateUrl: './accounts.component.html',
  styleUrls: ['./accounts.component.css']
})
export class AccountsComponent implements OnInit, OnDestroy {
  accounts: Account[] = [];
  selectedAccountId: number | null = null;
  loading = false;
  errorMessage = '';
  private refreshSub?: Subscription;

  // Modal State
  isModalOpen = false;
  modalTitle = 'Nova Conta';
  editingAccountId: number | null = null;
  formName = '';
  formType: AccountType = AccountType.Pessoal;
  formInitialBalance: number | null = 0;
  formColor = '#10B981';
  formIcon = 'person';
  modalLoading = false;
  modalError = '';

  AccountType = AccountType;

  // Color palette
  colorPalette = [
    '#10B981', '#059669', '#3B82F6', '#1D4ED8', '#6366F1',
    '#8B5CF6', '#8A05BE', '#EC4899', '#F43F5E', '#EF4444',
    '#F97316', '#F59E0B', '#0EA5E9', '#14B8A6', '#64748B'
  ];

  // Icons for Accounts
  availableIcons = [
    { id: 'person', label: 'Pessoal' },
    { id: 'building', label: 'Empresa / PJ' },
    { id: 'briefcase', label: 'Negócios' },
    { id: 'wallet2', label: 'Carteira' },
    { id: 'bank', label: 'Banco' },
    { id: 'cash-stack', label: 'Dinheiro' },
    { id: 'piggy-bank', label: 'Reserva' },
    { id: 'graph-up-arrow', label: 'Investimento' }
  ];

  constructor(private api: FinisApiService) {}

  ngOnInit(): void {
    this.selectedAccountId = this.api.getSelectedAccountId();
    this.loadAccounts();

    this.refreshSub = this.api.refresh$.subscribe(() => {
      this.selectedAccountId = this.api.getSelectedAccountId();
      this.loadAccounts();
    });
  }

  ngOnDestroy(): void {
    this.refreshSub?.unsubscribe();
  }

  loadAccounts(): void {
    this.loading = true;
    this.api.getAccounts().subscribe({
      next: data => {
        this.accounts = data;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.errorMessage = 'Erro ao carregar contas.';
      }
    });
  }

  getTotalInitialBalance(): number {
    return this.accounts.reduce((sum, a) => sum + (Number(a.initialBalance) || 0), 0);
  }

  getTotalCurrentBalance(): number {
    return this.accounts.reduce((sum, a) => sum + (Number(a.currentBalance) || 0), 0);
  }

  selectActiveView(accountId: number | null): void {
    this.api.setSelectedAccountId(accountId);
  }

  openNewModal(): void {
    this.modalTitle = 'Nova Conta / Carteira';
    this.editingAccountId = null;
    this.formName = '';
    this.formType = AccountType.Empresa;
    this.formInitialBalance = 0;
    this.formColor = '#3B82F6';
    this.formIcon = 'building';
    this.modalError = '';
    this.isModalOpen = true;
  }

  openEditModal(account: Account): void {
    this.modalTitle = 'Editar Conta';
    this.editingAccountId = account.id;
    this.formName = account.name;
    this.formType = account.type;
    this.formInitialBalance = account.initialBalance;
    this.formColor = account.colorHex || '#10B981';
    this.formIcon = account.icon || 'person';
    this.modalError = '';
    this.isModalOpen = true;
  }

  closeModal(): void {
    this.isModalOpen = false;
    this.editingAccountId = null;
  }

  saveAccount(): void {
    if (!this.formName.trim()) {
      this.modalError = 'Por favor, informe o nome da conta.';
      return;
    }

    this.modalLoading = true;
    this.modalError = '';

    const payload: AccountCreateUpdate = {
      name: this.formName.trim(),
      type: Number(this.formType),
      initialBalance: Number(this.formInitialBalance) || 0,
      colorHex: this.formColor,
      icon: this.formIcon,
      isActive: true
    };

    if (this.editingAccountId) {
      this.api.updateAccount(this.editingAccountId, payload).subscribe({
        next: () => {
          this.modalLoading = false;
          this.closeModal();
          this.loadAccounts();
        },
        error: err => {
          this.modalLoading = false;
          this.modalError = err.error?.message || 'Erro ao atualizar conta.';
        }
      });
    } else {
      this.api.createAccount(payload).subscribe({
        next: () => {
          this.modalLoading = false;
          this.closeModal();
          this.loadAccounts();
        },
        error: err => {
          this.modalLoading = false;
          this.modalError = err.error?.message || 'Erro ao criar conta.';
        }
      });
    }
  }

  deleteAccount(account: Account): void {
    if (this.accounts.length <= 1) {
      alert('Você precisa ter pelo menos uma conta ativa no sistema.');
      return;
    }

    if (confirm(`Deseja realmente excluir a conta "${account.name}"?\n\nOs lançamentos vinculados a ela não serão apagados, mas ficarão desvinculados.`)) {
      this.api.deleteAccount(account.id).subscribe({
        next: () => {
          this.loadAccounts();
        },
        error: err => {
          alert(err.error?.message || 'Erro ao excluir conta.');
        }
      });
    }
  }

  getTypeBadgeClass(type: AccountType): string {
    switch (type) {
      case AccountType.Pessoal: return 'bg-success-subtle text-success border-success-subtle';
      case AccountType.Empresa: return 'bg-primary-subtle text-primary border-primary-subtle';
      case AccountType.Investimento: return 'bg-info-subtle text-info border-info-subtle';
      default: return 'bg-secondary-subtle text-secondary border-secondary-subtle';
    }
  }
}
