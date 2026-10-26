import { Component, EventEmitter, OnDestroy, OnInit, Output } from '@angular/core';
import { Subscription } from 'rxjs';
import { FinisApiService } from '../../services/finis-api.service';
import { DailyFlow, MonthlyFlowResponse, Transaction } from '../../models/models';

@Component({
  selector: 'app-monthly-flow',
  templateUrl: './monthly-flow.component.html',
  styleUrls: ['./monthly-flow.component.css']
})
export class MonthlyFlowComponent implements OnInit, OnDestroy {
  flow: MonthlyFlowResponse | null = null;
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

  @Output() openNewTransaction = new EventEmitter<{ day?: number; date?: string }>();
  @Output() editTransaction = new EventEmitter<Transaction>();

  constructor(private api: FinisApiService) {}

  ngOnInit(): void {
    this.loadMonthlyFlow();
    this.refreshSub = this.api.refresh$.subscribe(() => {
      this.loadMonthlyFlow();
    });
  }

  ngOnDestroy(): void {
    this.refreshSub?.unsubscribe();
  }

  loadMonthlyFlow(): void {
    this.loading = true;
    this.api.getMonthlyFlow(this.selectedYear, this.selectedMonth).subscribe({
      next: data => {
        this.flow = data;
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
    this.loadMonthlyFlow();
  }

  onMonthSelect(m: number): void {
    this.selectedMonth = m;
    this.loadMonthlyFlow();
  }

  onYearSelect(y: number): void {
    this.selectedYear = y;
    this.loadMonthlyFlow();
  }

  toggleDay(day: DailyFlow): void {
    day.expanded = !day.expanded;
  }

  onAddForDay(day: DailyFlow, event: MouseEvent): void {
    event.stopPropagation();
    this.openNewTransaction.emit({ day: day.day, date: day.date });
  }

  onEdit(t: Transaction, event: MouseEvent): void {
    event.stopPropagation();
    this.editTransaction.emit(t);
  }

  onDelete(t: Transaction, event: MouseEvent): void {
    event.stopPropagation();
    if (confirm(`Deseja realmente excluir o lançamento "${t.description}" de R$ ${t.amount.toFixed(2)}?`)) {
      this.api.deleteTransaction(t.id).subscribe();
    }
  }

  isWeekend(dayOfWeek: string): boolean {
    return dayOfWeek === 'Sábado' || dayOfWeek === 'Domingo';
  }

  isToday(dateStr: string): boolean {
    const today = new Date();
    const d = new Date(dateStr);
    return today.getFullYear() === d.getFullYear() &&
           today.getMonth() === d.getMonth() &&
           today.getDate() === d.getDate();
  }
}
