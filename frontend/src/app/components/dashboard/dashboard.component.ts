import { Component, EventEmitter, OnDestroy, OnInit, Output } from '@angular/core';
import { Subscription } from 'rxjs';
import { FinisApiService } from '../../services/finis-api.service';
import { DashboardSummary, Transaction } from '../../models/models';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit, OnDestroy {
  summary: DashboardSummary | null = null;
  loading = false;
  selectedYear: number = new Date().getFullYear();
  selectedMonth: number = new Date().getMonth() + 1;
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

  onDelete(t: any): void {
    if (confirm(`Deseja realmente excluir o lançamento "${t.description}" de R$ ${Number(t.amount).toFixed(2)}?`)) {
      this.api.deleteTransaction(t.id).subscribe();
    }
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
