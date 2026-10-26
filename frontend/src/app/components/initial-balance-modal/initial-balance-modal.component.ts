import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-initial-balance-modal',
  templateUrl: './initial-balance-modal.component.html',
  styleUrls: ['./initial-balance-modal.component.css']
})
export class InitialBalanceModalComponent implements OnInit {
  @Input() isOpen = false;
  @Output() close = new EventEmitter<void>();
  @Output() updated = new EventEmitter<void>();

  balance: number = 0;
  loading = false;
  errorMessage = '';

  constructor(private authService: AuthService) {}

  ngOnInit(): void {
    this.authService.currentUser$.subscribe(user => {
      if (user) {
        this.balance = user.initialBalance;
      }
    });
  }

  onSubmit(): void {
    this.loading = true;
    this.errorMessage = '';

    this.authService.updateInitialBalance(Number(this.balance) || 0).subscribe({
      next: () => {
        this.loading = false;
        this.updated.emit();
        this.closeModal();
      },
      error: err => {
        this.loading = false;
        this.errorMessage = err.error?.message || 'Erro ao atualizar saldo inicial.';
      }
    });
  }

  closeModal(): void {
    this.close.emit();
  }
}
