import { Component, EventEmitter, Input, Output } from '@angular/core';
import { AuthService } from '../../services/auth.service';
import { FinisApiService } from '../../services/finis-api.service';

@Component({
  selector: 'app-reset-modal',
  templateUrl: './reset-modal.component.html',
  styleUrls: ['./reset-modal.component.css']
})
export class ResetModalComponent {
  @Input() isOpen = false;
  @Output() close = new EventEmitter<void>();
  @Output() resetCompleted = new EventEmitter<void>();

  confirmWord: string = '';
  loading = false;
  errorMessage = '';
  successMessage = '';

  constructor(
    private authService: AuthService,
    private api: FinisApiService
  ) {}

  resetState(): void {
    this.confirmWord = '';
    this.loading = false;
    this.errorMessage = '';
    this.successMessage = '';
  }

  closeModal(): void {
    this.resetState();
    this.close.emit();
  }

  onConfirmReset(): void {
    if (this.confirmWord.trim().toUpperCase() !== 'ZERAR') {
      this.errorMessage = 'Digite a palavra ZERAR exatamente em maiúsculas para confirmar.';
      return;
    }

    this.loading = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.authService.resetAllUserData().subscribe({
      next: () => {
        this.loading = false;
        this.successMessage = 'Todos os dados foram apagados com sucesso! O sistema foi reiniciado ao zero.';
        this.api.notifyDataChanged();
        this.resetCompleted.emit();

        setTimeout(() => {
          this.closeModal();
        }, 1800);
      },
      error: err => {
        this.loading = false;
        this.errorMessage = err.error?.message || 'Erro ao reiniciar dados. Tente novamente.';
      }
    });
  }
}
