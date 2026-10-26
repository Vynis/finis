import { Component, OnDestroy, OnInit } from '@angular/core';
import { Subscription } from 'rxjs';
import { FinisApiService } from '../../services/finis-api.service';
import { AnnualEconomyResponse } from '../../models/models';

@Component({
  selector: 'app-economy-report',
  templateUrl: './economy-report.component.html',
  styleUrls: ['./economy-report.component.css']
})
export class EconomyReportComponent implements OnInit, OnDestroy {
  economy: AnnualEconomyResponse | null = null;
  loading = false;
  selectedYear: number = new Date().getFullYear();
  availableYears: number[] = [2024, 2025, 2026, 2027];
  private refreshSub?: Subscription;

  constructor(private api: FinisApiService) {}

  ngOnInit(): void {
    this.loadEconomyReport();
    this.refreshSub = this.api.refresh$.subscribe(() => {
      this.loadEconomyReport();
    });
  }

  ngOnDestroy(): void {
    this.refreshSub?.unsubscribe();
  }

  loadEconomyReport(): void {
    this.loading = true;
    this.api.getAnnualEconomy(this.selectedYear).subscribe({
      next: data => {
        this.economy = data;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  onYearChange(year: number): void {
    this.selectedYear = year;
    this.loadEconomyReport();
  }
}
