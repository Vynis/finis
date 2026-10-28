import { Component, EventEmitter, OnDestroy, OnInit, Output } from '@angular/core';
import { Subscription } from 'rxjs';
import { FinisApiService } from '../../services/finis-api.service';
import { AdvisorAnalysis, DashboardSummary, Transaction } from '../../models/models';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit, OnDestroy {
  summary: DashboardSummary | null = null;
  advisor: AdvisorAnalysis | null = null;
  loading = false;
  selectedYear: number = new Date().getFullYear();
  selectedMonth: number = new Date().getMonth() + 1;
  showOverdueDetails: boolean = false;
  private refreshSub?: Subscription;

  availableYears: number[] = [2024, 2025, 2026, 2027];
  months = [
    { value: 1, name: 'Janeiro' },
    { value: 2, name: 'Fevereiro' },
    { value: 3, name: 'Março' },
    { value: 4, name: 'Abril' },
    { value: 5, name: 'Maio' },
    { value: 6, name: 'Junho' },
    { value: 7, name: 'Julho' },
    { value: 8, name: 'Agosto' },
    { value: 9, name: 'Setembro' },
    { value: 10, name: 'Outubro' },
    { value: 11, name: 'Novembro' },
    { value: 12, name: 'Dezembro' }
  ];

  @Output() openNewTransaction = new EventEmitter<void>();
  @Output() editTransaction = new EventEmitter<Transaction>();

  constructor(private api: FinisApiService) {}

  ngOnInit(): void {
    this.loadDashboard();
    // Automatically re-query dashboard whenever any transaction is created, updated, or deleted
    this.refreshSub = this.api.refresh$.subscribe(() => {
      this.loadDashboard();
    });
  }

  ngOnDestroy(): void {
    this.refreshSub?.unsubscribe();
  }

  loadDashboard(): void {
    this.loading = true;
    this.api.getDashboardSummary(this.selectedYear, this.selectedMonth).subscribe({
      next: data => {
        this.summary = data;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });

    this.api.getAdvisorAnalysis(this.selectedYear, this.selectedMonth).subscribe({
      next: adv => {
        this.advisor = adv;
      }
    });
  }

  changeMonth(delta: number): void {
    let newMonth = this.selectedMonth + delta;
    if (newMonth < 1) {
      newMonth = 12;
      this.selectedYear--;
    } else if (newMonth > 12) {
      newMonth = 1;
      this.selectedYear++;
    }
    this.selectedMonth = newMonth;
    this.loadDashboard();
  }

  onMonthSelect(m: number): void {
    this.selectedMonth = m;
    this.loadDashboard();
  }

  onYearSelect(y: number): void {
    this.selectedYear = y;
    this.loadDashboard();
  }

  onEdit(t: any): void {
    this.editTransaction.emit(t);
  }

  onToggleStatus(t: any): void {
    if (t.id <= 0) return;
    this.api.toggleTransactionStatus(t.id).subscribe();
  }

  onDelete(t: any): void {
    if (t.recurrenceGroupId || t.isRecurring) {
      const ans = confirm(`"${t.description}" faz parte de uma série recorrente.\n\nDeseja excluir também as repetições futuras deste lançamento?\n\n• Clique [OK] para excluir este e os futuros.\n• Clique [Cancelar] se deseja excluir apenas este mês.`);
      if (ans) {
        this.api.deleteTransaction(t.id, { deleteFuture: true }).subscribe();
      } else {
        if (confirm(`Deseja excluir APENAS o lançamento deste mês ("${t.description}")?`)) {
          this.api.deleteTransaction(t.id).subscribe();
        }
      }
    } else {
      if (confirm(`Deseja realmente excluir o lançamento "${t.description}" de R$ ${Number(t.amount).toFixed(2)}?`)) {
        this.api.deleteTransaction(t.id).subscribe();
      }
    }
  }

  toggleOverdueDetails(): void {
    this.showOverdueDetails = !this.showOverdueDetails;
  }

  getOverdueLabel(dateVal: string | Date): string {
    const d = new Date(dateVal);
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    d.setHours(0, 0, 0, 0);
    const diffTime = today.getTime() - d.getTime();
    const diffDays = Math.floor(diffTime / (1000 * 60 * 60 * 24));
    if (diffDays <= 0) return 'Vence hoje';
    if (diffDays === 1) return 'Venceu ontem';
    return `${diffDays} dias de atraso`;
  }

  isOverdue(dateVal: string | Date, status: number): boolean {
    if (status !== 2) return false;
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const d = new Date(dateVal);
    d.setHours(0, 0, 0, 0);
    return d.getTime() < today.getTime();
  }

  getIncomeProgress(): number {
    if (!this.summary || this.summary.monthIncome <= 0) return 0;
    const pct = Math.round(((this.summary.realizedIncome || 0) / this.summary.monthIncome) * 100);
    return Math.min(Math.max(pct, 0), 100);
  }

  getExpensesProgress(): number {
    if (!this.summary || this.summary.monthTotalExpenses <= 0) return 0;
    const pct = Math.round(((this.summary.realizedExpenses || 0) / this.summary.monthTotalExpenses) * 100);
    return Math.min(Math.max(pct, 0), 100);
  }

  // Helper for SVG chart points
  getChartPoints(): string {
    if (!this.summary || !this.summary.dailyTrend || this.summary.dailyTrend.length === 0) {
      return '';
    }

    const trend = this.summary.dailyTrend;
    const values = trend.map(t => Number(t.balance));
    const minVal = Math.min(...values, 0);
    const maxVal = Math.max(...values, 100);
    const range = (maxVal - minVal) || 1;

    const width = 800;
    const height = 220;
    const padding = 20;

    return trend.map((point, index) => {
      const x = padding + (index / (trend.length - 1 || 1)) * (width - 2 * padding);
      const y = height - padding - ((Number(point.balance) - minVal) / range) * (height - 2 * padding);
      return `${x},${y}`;
    }).join(' ');
  }
}
