import { Component, EventEmitter, Input, OnDestroy, OnInit, Output } from '@angular/core';
import { Subscription } from 'rxjs';
import { FinisApiService } from '../../services/finis-api.service';
import { Category, CreditCard, PaymentMethod, Transaction, TransactionCreateUpdate, TransactionStatus, TransactionType } from '../../models/models';

@Component({
  selector: 'app-transaction-modal',
  templateUrl: './transaction-modal.component.html',
  styleUrls: ['./transaction-modal.component.css']
})
export class TransactionModalComponent implements OnInit, OnDestroy {
  @Input() isOpen = false;
  @Input() transactionToEdit: Transaction | null = null;
  @Input() initialDate: string | null = null;

  @Output() close = new EventEmitter<void>();
  @Output() saved = new EventEmitter<void>();

  categories: Category[] = [];
  filteredCategories: Category[] = [];
  otherCategories: Category[] = [];
  creditCards: CreditCard[] = [];

  // Form Model
  type: TransactionType = TransactionType.Diario;
  date: string = new Date().toISOString().substring(0, 10);
  amount: number | null = null;
  description: string = '';
  categoryId: number | null = null;
  paymentMethod: PaymentMethod = PaymentMethod.Pix;
  status: TransactionStatus = TransactionStatus.Realizado;
  notes: string = '';

  // Credit Card fields
  creditCardId: number | null = null;
  installments: number = 1;

  // Recurring / Fixed monthly options
  isRecurring: boolean = false;
  recurrenceMonths: number = 12;

  loading = false;
  errorMessage = '';

  private refreshSub?: Subscription;

  // Enums for template
  TransactionType = TransactionType;
  TransactionStatus = TransactionStatus;
  PaymentMethod = PaymentMethod;

  constructor(private api: FinisApiService) {}

  ngOnInit(): void {
    this.loadCategories();
    this.loadCreditCards();

    // Auto-refresh categories and cards whenever any changes occur in the system
    this.refreshSub = this.api.refresh$.subscribe(() => {
      this.loadCategories();
      this.loadCreditCards();
    });
  }

  ngOnDestroy(): void {
    this.refreshSub?.unsubscribe();
  }

  loadCategories(): void {
    this.api.getCategories().subscribe({
      next: cats => {
        this.categories = cats;
        this.filterCategories();
      }
    });
  }

  loadCreditCards(): void {
    this.api.getCreditCards().subscribe({
      next: cards => {
        this.creditCards = cards;
        if (!this.creditCardId && cards.length > 0) {
          this.creditCardId = cards[0].id;
        }
      }
    });
  }

  // Open / Reset form
  resetForm(presetDate?: string, editData?: Transaction | null): void {
    this.errorMessage = '';
    this.loading = false;
    this.loadCategories();
    this.loadCreditCards();

    if (editData) {
      this.type = editData.type;
      this.date = editData.date.substring(0, 10);
      this.amount = editData.amount;
      this.description = editData.description;
      this.categoryId = editData.categoryId || null;
      this.paymentMethod = editData.paymentMethod;
      this.status = editData.status;
      this.notes = editData.notes || '';
      this.creditCardId = editData.creditCardId || (this.creditCards.length > 0 ? this.creditCards[0].id : null);
      this.installments = editData.totalInstallments || 1;
      this.isRecurring = false;
      this.recurrenceMonths = 12;
    } else {
      this.type = TransactionType.Diario;
      this.date = presetDate ? presetDate.substring(0, 10) : new Date().toISOString().substring(0, 10);
      this.amount = null;
      this.description = '';
      this.categoryId = null;
      this.paymentMethod = PaymentMethod.Pix;
      this.status = TransactionStatus.Realizado;
      this.notes = '';
      this.creditCardId = this.creditCards.length > 0 ? this.creditCards[0].id : null;
      this.installments = 1;
      this.isRecurring = false;
      this.recurrenceMonths = 12;
    }

    this.filterCategories();
  }

  onTypeChange(newType: TransactionType): void {
    this.type = newType;
    this.filterCategories();
  }

  filterCategories(): void {
    const curType = Number(this.type);
    this.filteredCategories = this.categories.filter(c => Number(c.type) === curType);
    this.otherCategories = this.categories.filter(c => Number(c.type) !== curType);

    if (this.categoryId && !this.categories.some(c => c.id === this.categoryId)) {
      this.categoryId = null;
    }
  }

  onCategoryChange(): void {
    if (this.categoryId) {
      const selected = this.categories.find(c => c.id === this.categoryId);
      if (selected && Number(selected.type) !== Number(this.type)) {
        // Automatically align transaction type with the category
        this.type = selected.type;
        this.filterCategories();
      }
    }
  }

  getTypeName(t: TransactionType | number): string {
    switch (Number(t)) {
      case TransactionType.Entrada: return 'Entrada';
      case TransactionType.SaidaFixa: return 'Saída Fixa';
      case TransactionType.Diario: return 'Gasto Diário';
      default: return 'Geral';
    }
  }

  getSelectedCard(): CreditCard | undefined {
    return this.creditCards.find(c => c.id === this.creditCardId);
  }

  getInstallmentPreview(): string {
    if (!this.amount || this.installments <= 1) return '';
    const perInst = (this.amount / this.installments).toFixed(2);
    return `${this.installments}x de R$ ${perInst}`;
  }

  onSubmit(): void {
    if (!this.description || !this.amount || this.amount <= 0 || !this.date) {
      this.errorMessage = 'Por favor, informe uma descrição, valor válido e data.';
      return;
    }

    if (this.paymentMethod === PaymentMethod.CartaoCredito && this.creditCards.length > 0 && !this.creditCardId) {
      this.errorMessage = 'Por favor, selecione qual cartão de crédito foi utilizado.';
      return;
    }

    this.loading = true;
    this.errorMessage = '';

    const isCard = Number(this.paymentMethod) === PaymentMethod.CartaoCredito;

    const payload: TransactionCreateUpdate = {
      type: this.type,
      date: new Date(this.date + 'T12:00:00Z').toISOString(),
      amount: Number(this.amount),
      description: this.description.trim(),
      categoryId: this.categoryId ? Number(this.categoryId) : undefined,
      paymentMethod: Number(this.paymentMethod),
      status: Number(this.status),
      notes: this.notes ? this.notes.trim() : undefined,
      creditCardId: (isCard && this.creditCardId) ? Number(this.creditCardId) : undefined,
      installments: isCard ? Math.max(1, Number(this.installments) || 1) : undefined,
      isRecurring: (!isCard && !this.transactionToEdit && this.isRecurring) ? true : undefined,
      recurrenceMonths: (!isCard && !this.transactionToEdit && this.isRecurring) ? Math.max(2, Number(this.recurrenceMonths) || 12) : undefined
    };

    if (this.transactionToEdit) {
      this.api.updateTransaction(this.transactionToEdit.id, payload).subscribe({
        next: () => {
          this.loading = false;
          this.saved.emit();
          this.closeModal();
        },
        error: err => {
          this.loading = false;
          this.errorMessage = err.error?.message || 'Erro ao atualizar lançamento.';
        }
      });
    } else {
      this.api.createTransaction(payload).subscribe({
        next: () => {
          this.loading = false;
          this.saved.emit();
          this.closeModal();
        },
        error: err => {
          this.loading = false;
          this.errorMessage = err.error?.message || 'Erro ao salvar lançamento.';
        }
      });
    }
  }

  closeModal(): void {
    this.close.emit();
  }
}
