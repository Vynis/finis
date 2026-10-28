import { Component, OnDestroy, OnInit } from '@angular/core';
import { Subscription } from 'rxjs';
import { FinisApiService } from '../../services/finis-api.service';
import { Account, AdvisorAnalysis, Debt, DebtCreateUpdate, Goal, GoalCreateUpdate } from '../../models/models';

@Component({
  selector: 'app-goals-debts',
  templateUrl: './goals-debts.component.html',
  styleUrls: ['./goals-debts.component.css']
})
export class GoalsDebtsComponent implements OnInit, OnDestroy {
  activeTab: 'advisor' | 'goals' | 'debts' = 'advisor';
  loading = false;

  advisor: AdvisorAnalysis | null = null;
  goals: Goal[] = [];
  debts: Debt[] = [];
  accounts: Account[] = [];

  selectedYear: number = new Date().getFullYear();
  selectedMonth: number = new Date().getMonth() + 1;

  // Simulator
  simulateExtraAmount: number = 500;

  // Modals state
  isGoalModalOpen = false;
  editingGoalId: number | null = null;
  goalName = '';
  goalAccountId: number | null = null;
  goalTargetAmount: number | null = null;
  goalCurrentAmount: number | null = 0;
  goalTargetDate = '';
  goalCategory = 'Reserva de Emergência';
  goalColor = '#10B981';
  goalIcon = 'bullseye';
  goalPriority = 1;
  goalLoading = false;
  goalError = '';

  isDebtModalOpen = false;
  editingDebtId: number | null = null;
  debtName = '';
  debtCreditor = '';
  debtAccountId: number | null = null;
  debtTotalAmount: number | null = null;
  debtRemainingAmount: number | null = null;
  debtInterestRate: number | null = 2.5;
  debtMonthlyPayment: number | null = null;
  debtDueDay: number = 10;
  debtColor = '#EF4444';
  debtNotes = '';
  debtLoading = false;
  debtError = '';

  // Quick Action Modal (Deposit/Withdraw Goal or Amortize Debt)
  isQuickActionModalOpen = false;
  quickActionType: 'depositGoal' | 'withdrawGoal' | 'amortizeDebt' = 'depositGoal';
  quickActionTitle = '';
  quickActionTargetName = '';
  quickActionAmount: number | null = null;
  selectedGoal?: Goal;
  selectedDebt?: Debt;
  quickActionLoading = false;
  quickActionError = '';

  private refreshSub?: Subscription;

  goalCategories = [
    { name: 'Reserva de Emergência', icon: 'shield-check', color: '#10B981' },
    { name: 'Viagem & Férias', icon: 'airplane', color: '#3B82F6' },
    { name: 'Veículo / Carro', icon: 'car-front', color: '#6366F1' },
    { name: 'Imóvel / Casa Própria', icon: 'house-heart', color: '#8B5CF6' },
    { name: 'Investimento / Futuro', icon: 'graph-up-arrow', color: '#059669' },
    { name: 'Educação / Cursos', icon: 'mortarboard', color: '#F59E0B' },
    { name: 'Eletrônicos & Compras', icon: 'laptop', color: '#EC4899' },
    { name: 'Outro Objetivo', icon: 'bullseye', color: '#64748B' }
  ];

  constructor(private api: FinisApiService) {}

  ngOnInit(): void {
    this.loadAll();
    this.refreshSub = this.api.refresh$.subscribe(() => {
      this.loadAll();
    });
  }

  ngOnDestroy(): void {
    this.refreshSub?.unsubscribe();
  }

  loadAll(): void {
    this.loading = true;
    this.loadAccounts();
    this.loadAdvisor();
    this.loadGoals();
    this.loadDebts();
  }

  loadAdvisor(): void {
    this.api.getAdvisorAnalysis(this.selectedYear, this.selectedMonth).subscribe({
      next: data => {
        this.advisor = data;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  loadGoals(): void {
    this.api.getGoals().subscribe({
      next: data => {
        this.goals = data;
      }
    });
  }

  loadDebts(): void {
    this.api.getDebts().subscribe({
      next: data => {
        this.debts = data;
      }
    });
  }

  loadAccounts(): void {
    this.api.getAccounts().subscribe({
      next: data => {
        this.accounts = data;
      }
    });
  }

  setTab(tab: 'advisor' | 'goals' | 'debts'): void {
    this.activeTab = tab;
  }

  // --- Goal Modal ---
  openCreateGoalModal(): void {
    this.editingGoalId = null;
    this.goalName = '';
    this.goalAccountId = this.accounts.length > 0 ? this.accounts[0].id : null;
    this.goalTargetAmount = null;
    this.goalCurrentAmount = 0;
    this.goalTargetDate = '';
    this.goalCategory = 'Reserva de Emergência';
    this.goalColor = '#10B981';
    this.goalIcon = 'shield-check';
    this.goalPriority = 1;
    this.goalError = '';
    this.isGoalModalOpen = true;
  }

  openEditGoalModal(g: Goal): void {
    this.editingGoalId = g.id;
    this.goalName = g.name;
    this.goalAccountId = g.accountId || null;
    this.goalTargetAmount = g.targetAmount;
    this.goalCurrentAmount = g.currentAmount;
    this.goalTargetDate = g.targetDate ? g.targetDate.substring(0, 10) : '';
    this.goalCategory = g.category;
    this.goalColor = g.colorHex;
    this.goalIcon = g.icon;
    this.goalPriority = g.priority;
    this.goalError = '';
    this.isGoalModalOpen = true;
  }

  closeGoalModal(): void {
    this.isGoalModalOpen = false;
    this.editingGoalId = null;
  }

  onSelectGoalCategory(cat: { name: string; icon: string; color: string }): void {
    this.goalCategory = cat.name;
    this.goalIcon = cat.icon;
    this.goalColor = cat.color;
  }

  onSaveGoal(): void {
    if (!this.goalName.trim() || !this.goalTargetAmount || this.goalTargetAmount <= 0) {
      this.goalError = 'Informe um nome e um valor alvo válido.';
      return;
    }

    this.goalLoading = true;
    this.goalError = '';

    const payload: GoalCreateUpdate = {
      name: this.goalName.trim(),
      accountId: this.goalAccountId || undefined,
      targetAmount: this.goalTargetAmount,
      currentAmount: this.goalCurrentAmount || 0,
      targetDate: this.goalTargetDate ? new Date(this.goalTargetDate).toISOString() : undefined,
      category: this.goalCategory,
      colorHex: this.goalColor,
      icon: this.goalIcon,
      priority: this.goalPriority
    };

    if (this.editingGoalId) {
      this.api.updateGoal(this.editingGoalId, payload).subscribe({
        next: () => {
          this.goalLoading = false;
          this.closeGoalModal();
          this.loadAll();
        },
        error: err => {
          this.goalLoading = false;
          this.goalError = err.error?.message || 'Erro ao atualizar meta.';
        }
      });
    } else {
      this.api.createGoal(payload).subscribe({
        next: () => {
          this.goalLoading = false;
          this.closeGoalModal();
          this.loadAll();
        },
        error: err => {
          this.goalLoading = false;
          this.goalError = err.error?.message || 'Erro ao criar meta.';
        }
      });
    }
  }

  onDeleteGoal(g: Goal): void {
    if (confirm(`Deseja realmente excluir a meta "${g.name}"?`)) {
      this.api.deleteGoal(g.id).subscribe({
        next: () => this.loadAll()
      });
    }
  }

  // --- Debt Modal ---
  openCreateDebtModal(): void {
    this.editingDebtId = null;
    this.debtName = '';
    this.debtCreditor = '';
    this.debtAccountId = this.accounts.length > 0 ? this.accounts[0].id : null;
    this.debtTotalAmount = null;
    this.debtRemainingAmount = null;
    this.debtInterestRate = 2.5;
    this.debtMonthlyPayment = null;
    this.debtDueDay = 10;
    this.debtColor = '#EF4444';
    this.debtNotes = '';
    this.debtError = '';
    this.isDebtModalOpen = true;
  }

  openEditDebtModal(d: Debt): void {
    this.editingDebtId = d.id;
    this.debtName = d.name;
    this.debtCreditor = d.creditor || '';
    this.debtAccountId = d.accountId || null;
    this.debtTotalAmount = d.totalAmount;
    this.debtRemainingAmount = d.remainingAmount;
    this.debtInterestRate = d.interestRateMonthly;
    this.debtMonthlyPayment = d.monthlyPayment;
    this.debtDueDay = d.dueDay;
    this.debtColor = d.colorHex;
    this.debtNotes = d.notes || '';
    this.debtError = '';
    this.isDebtModalOpen = true;
  }

  closeDebtModal(): void {
    this.isDebtModalOpen = false;
    this.editingDebtId = null;
  }

  onSaveDebt(): void {
    if (!this.debtName.trim() || !this.debtTotalAmount || this.debtTotalAmount <= 0) {
      this.debtError = 'Informe um nome e o valor total da dívida.';
      return;
    }

    const remaining = this.debtRemainingAmount !== null ? this.debtRemainingAmount : this.debtTotalAmount;

    this.debtLoading = true;
    this.debtError = '';

    const payload: DebtCreateUpdate = {
      name: this.debtName.trim(),
      creditor: this.debtCreditor.trim() || undefined,
      accountId: this.debtAccountId || undefined,
      totalAmount: this.debtTotalAmount,
      remainingAmount: remaining,
      interestRateMonthly: this.debtInterestRate || 0,
      monthlyPayment: this.debtMonthlyPayment || 0,
      dueDay: this.debtDueDay || 10,
      colorHex: this.debtColor,
      notes: this.debtNotes.trim() || undefined
    };

    if (this.editingDebtId) {
      this.api.updateDebt(this.editingDebtId, payload).subscribe({
        next: () => {
          this.debtLoading = false;
          this.closeDebtModal();
          this.loadAll();
        },
        error: err => {
          this.debtLoading = false;
          this.debtError = err.error?.message || 'Erro ao atualizar dívida.';
        }
      });
    } else {
      this.api.createDebt(payload).subscribe({
        next: () => {
          this.debtLoading = false;
          this.closeDebtModal();
          this.loadAll();
        },
        error: err => {
          this.debtLoading = false;
          this.debtError = err.error?.message || 'Erro ao cadastrar dívida.';
        }
      });
    }
  }

  onDeleteDebt(d: Debt): void {
    if (confirm(`Deseja realmente excluir a dívida "${d.name}"?`)) {
      this.api.deleteDebt(d.id).subscribe({
        next: () => this.loadAll()
      });
    }
  }

  // --- Quick Actions ---
  openDepositGoalModal(g: Goal, isWithdraw = false): void {
    this.selectedGoal = g;
    this.quickActionType = isWithdraw ? 'withdrawGoal' : 'depositGoal';
    this.quickActionTitle = isWithdraw ? 'Retirar da Meta' : 'Aportar na Meta';
    this.quickActionTargetName = g.name;
    this.quickActionAmount = null;
    this.quickActionError = '';
    this.isQuickActionModalOpen = true;
  }

  openAmortizeDebtModal(d: Debt): void {
    this.selectedDebt = d;
    this.quickActionType = 'amortizeDebt';
    this.quickActionTitle = 'Amortizar / Abater Dívida';
    this.quickActionTargetName = d.name;
    this.quickActionAmount = null;
    this.quickActionError = '';
    this.isQuickActionModalOpen = true;
  }

  closeQuickActionModal(): void {
    this.isQuickActionModalOpen = false;
    this.selectedGoal = undefined;
    this.selectedDebt = undefined;
    this.quickActionAmount = null;
  }

  onConfirmQuickAction(): void {
    if (!this.quickActionAmount || this.quickActionAmount <= 0) {
      this.quickActionError = 'Informe um valor válido maior que zero.';
      return;
    }

    this.quickActionLoading = true;
    this.quickActionError = '';

    if (this.quickActionType === 'depositGoal' && this.selectedGoal) {
      this.api.depositGoal(this.selectedGoal.id, this.quickActionAmount, false).subscribe({
        next: () => {
          this.quickActionLoading = false;
          this.closeQuickActionModal();
          this.loadAll();
        },
        error: err => {
          this.quickActionLoading = false;
          this.quickActionError = err.error?.message || 'Erro ao realizar aporte.';
        }
      });
    } else if (this.quickActionType === 'withdrawGoal' && this.selectedGoal) {
      this.api.depositGoal(this.selectedGoal.id, this.quickActionAmount, true).subscribe({
        next: () => {
          this.quickActionLoading = false;
          this.closeQuickActionModal();
          this.loadAll();
        },
        error: err => {
          this.quickActionLoading = false;
          this.quickActionError = err.error?.message || 'Erro ao realizar retirada.';
        }
      });
    } else if (this.quickActionType === 'amortizeDebt' && this.selectedDebt) {
      this.api.amortizeDebt(this.selectedDebt.id, this.quickActionAmount).subscribe({
        next: () => {
          this.quickActionLoading = false;
          this.closeQuickActionModal();
          this.loadAll();
        },
        error: err => {
          this.quickActionLoading = false;
          this.quickActionError = err.error?.message || 'Erro ao amortizar dívida.';
        }
      });
    }
  }

  // Simulator calculation
  hasExpensiveDebts(): boolean {
    return this.debts.some(d => !d.isSettled && d.interestRateMonthly > 0);
  }

  getSimulatedDebtSavings(): number {
    if (!this.debts || this.debts.length === 0 || !this.simulateExtraAmount || this.simulateExtraAmount <= 0) {
      return 0;
    }
    const worstDebt = this.debts.find(d => !d.isSettled && d.interestRateMonthly > 0);
    if (!worstDebt) return 0;
    const amortized = Math.min(worstDebt.remainingAmount, this.simulateExtraAmount);
    // 6-month compound interest savings approx
    return Math.round(amortized * (worstDebt.interestRateMonthly / 100) * 6);
  }

  getSimulatedSnowballImpact(): { amortized: number; message: string } {
    if (!this.debts || this.debts.length === 0 || !this.simulateExtraAmount || this.simulateExtraAmount <= 0) {
      return { amortized: 0, message: '' };
    }
    const activeDebts = this.debts.filter(d => !d.isSettled && d.remainingAmount > 0);
    if (activeDebts.length === 0) return { amortized: 0, message: '' };

    const smallestDebt = [...activeDebts].sort((a, b) => a.remainingAmount - b.remainingAmount)[0];
    const amortized = Math.min(smallestDebt.remainingAmount, this.simulateExtraAmount);

    if (this.simulateExtraAmount >= smallestDebt.remainingAmount) {
      return { amortized, message: `Quita 100% de ${smallestDebt.name}!` };
    } else {
      const pct = Math.round((amortized / smallestDebt.remainingAmount) * 100);
      return { amortized, message: `Abate ${pct}% de ${smallestDebt.name}` };
    }
  }
}
