import { Component, EventEmitter, OnDestroy, OnInit, Output } from '@angular/core';
import { Subscription } from 'rxjs';
import { FinisApiService } from '../../services/finis-api.service';
import { Account, CreditCard, CreditCardCreateUpdate, CreditCardInvoiceSummary, PaymentMethod, Transaction } from '../../models/models';

@Component({
  selector: 'app-credit-cards',
  templateUrl: './credit-cards.component.html',
  styleUrls: ['./credit-cards.component.css']
})
export class CreditCardsComponent implements OnInit, OnDestroy {
  cards: CreditCard[] = [];
  selectedCard: CreditCard | null = null;
  upcomingInvoices: CreditCardInvoiceSummary[] = [];
  selectedInvoice: CreditCardInvoiceSummary | null = null;

  loadingCards = false;
  loadingInvoices = false;
  errorMessage = '';
  successMessage = '';

  @Output() editTransaction = new EventEmitter<Transaction>();

  private refreshSub?: Subscription;

  // Card Modal (Create / Edit)
  isCardModalOpen = false;
  cardToEdit: CreditCard | null = null;
  formName = '';
  formLimit: number | null = null;
  formClosingDay = 25;
  formDueDay = 5;
  formColor = '#8A05BE';
  formBrand = 'Mastercard';
  cardSaving = false;
  cardError = '';

  // Pay Invoice Modal
  isPayModalOpen = false;
  invoiceToPay: CreditCardInvoiceSummary | null = null;
  payMethod: PaymentMethod = PaymentMethod.Pix;
  payDate: string = new Date().toISOString().substring(0, 10);
  payAmount: number | null = null;
  paying = false;
  payError = '';

  // Preset Colors
  paletteColors = [
    { name: 'Nubank Roxo', hex: '#8A05BE' },
    { name: 'Itaú Laranja', hex: '#EC7000' },
    { name: 'Bradesco Vermelho', hex: '#CC092F' },
    { name: 'Santander Vermelho', hex: '#EC0000' },
    { name: 'Inter Laranja', hex: '#FF7A00' },
    { name: 'C6 Bank Grafite', hex: '#242424' },
    { name: 'BTG Azul Marinho', hex: '#001E62' },
    { name: 'XP Investimentos', hex: '#1C1C1C' },
    { name: 'Banco do Brasil', hex: '#F9A825' },
    { name: 'Caixa Azul', hex: '#006699' },
    { name: 'Verde Esmeralda', hex: '#10B981' },
    { name: 'Azul Turquesa', hex: '#0EA5E9' }
  ];

  brands = ['Mastercard', 'Visa', 'Elo', 'American Express', 'Hipercard', 'Outro'];
  accounts: Account[] = [];
  formAccountId: number | null = null;

  constructor(private api: FinisApiService) {}

  ngOnInit(): void {
    this.loadCards();
    this.loadAccounts();
    this.refreshSub = this.api.refresh$.subscribe(() => {
      this.loadCards(true);
      this.loadAccounts();
    });
  }

  loadAccounts(): void {
    this.api.getAccounts().subscribe({
      next: accs => this.accounts = accs
    });
  }

  ngOnDestroy(): void {
    this.refreshSub?.unsubscribe();
  }

  loadCards(isSilent = false): void {
    if (!isSilent) this.loadingCards = true;
    this.api.getCreditCards().subscribe({
      next: cards => {
        this.cards = cards;
        this.loadingCards = false;

        if (this.cards.length > 0) {
          const currentId = this.selectedCard ? this.selectedCard.id : this.cards[0].id;
          const matched = this.cards.find(c => c.id === currentId) || this.cards[0];
          this.selectCard(matched);
        } else {
          this.selectedCard = null;
          this.upcomingInvoices = [];
          this.selectedInvoice = null;
        }
      },
      error: () => {
        this.loadingCards = false;
      }
    });
  }

  selectCard(card: CreditCard): void {
    this.selectedCard = card;
    this.loadInvoices(card.id);
  }

  loadInvoices(cardId: number): void {
    this.loadingInvoices = true;
    this.api.getUpcomingInvoices(cardId, 6).subscribe({
      next: invoices => {
        this.upcomingInvoices = invoices;
        this.loadingInvoices = false;
        if (invoices.length > 0) {
          // Select open invoice or first one
          this.selectedInvoice = invoices.find(inv => inv.status === 'Aberta') || invoices[0];
        } else {
          this.selectedInvoice = null;
        }
      },
      error: () => {
        this.loadingInvoices = false;
      }
    });
  }

  selectInvoice(inv: CreditCardInvoiceSummary): void {
    this.selectedInvoice = inv;
  }

  // Card Management Modal
  openNewCardModal(): void {
    this.cardToEdit = null;
    this.formName = '';
    this.formLimit = 2000;
    this.formClosingDay = 25;
    this.formDueDay = 5;
    this.formColor = '#8A05BE';
    this.formBrand = 'Mastercard';
    this.formAccountId = this.api.getSelectedAccountId() || (this.accounts.length > 0 ? this.accounts[0].id : null);
    this.cardError = '';
    this.cardSaving = false;
    this.isCardModalOpen = true;
  }

  openEditCardModal(card: CreditCard): void {
    this.cardToEdit = card;
    this.formName = card.name;
    this.formLimit = card.limit;
    this.formClosingDay = card.closingDay;
    this.formDueDay = card.dueDay;
    this.formColor = card.colorHex;
    this.formBrand = card.brand;
    this.formAccountId = card.accountId || null;
    this.cardError = '';
    this.cardSaving = false;
    this.isCardModalOpen = true;
  }

  closeCardModal(): void {
    this.isCardModalOpen = false;
    this.cardToEdit = null;
  }

  saveCard(): void {
    if (!this.formName || !this.formLimit || this.formLimit <= 0) {
      this.cardError = 'Informe o nome e um limite válido.';
      return;
    }

    if (this.formClosingDay < 1 || this.formClosingDay > 31 || this.formDueDay < 1 || this.formDueDay > 31) {
      this.cardError = 'Os dias de fechamento e vencimento devem ser entre 1 e 31.';
      return;
    }

    this.cardSaving = true;
    this.cardError = '';

    const payload: CreditCardCreateUpdate = {
      name: this.formName.trim(),
      limit: Number(this.formLimit),
      closingDay: Number(this.formClosingDay),
      dueDay: Number(this.formDueDay),
      colorHex: this.formColor,
      brand: this.formBrand,
      accountId: this.formAccountId ? Number(this.formAccountId) : undefined,
      isActive: true
    };

    if (this.cardToEdit) {
      this.api.updateCreditCard(this.cardToEdit.id, payload).subscribe({
        next: updated => {
          this.cardSaving = false;
          this.closeCardModal();
          this.loadCards();
        },
        error: err => {
          this.cardSaving = false;
          this.cardError = err.error?.message || 'Erro ao atualizar cartão.';
        }
      });
    } else {
      this.api.createCreditCard(payload).subscribe({
        next: created => {
          this.cardSaving = false;
          this.closeCardModal();
          this.loadCards();
        },
        error: err => {
          this.cardSaving = false;
          this.cardError = err.error?.message || 'Erro ao cadastrar cartão.';
        }
      });
    }
  }

  deleteCard(card: CreditCard): void {
    if (confirm(`Deseja realmente excluir o cartão "${card.name}"? As transações existentes permanecerão registradas no histórico.`)) {
      this.api.deleteCreditCard(card.id).subscribe({
        next: () => {
          this.loadCards();
        }
      });
    }
  }

  // Pay Invoice Modal
  openPayInvoiceModal(invoice: CreditCardInvoiceSummary): void {
    this.invoiceToPay = invoice;
    this.payMethod = PaymentMethod.Pix;
    this.payDate = invoice.dueDate.substring(0, 10);
    this.payAmount = invoice.totalAmount;
    this.payError = '';
    this.paying = false;
    this.isPayModalOpen = true;
  }

  closePayModal(): void {
    this.isPayModalOpen = false;
    this.invoiceToPay = null;
  }

  confirmPayInvoice(): void {
    if (!this.invoiceToPay || !this.selectedCard) return;

    if (!this.payAmount || this.payAmount <= 0) {
      this.payError = 'Informe um valor de pagamento válido.';
      return;
    }

    this.paying = true;
    this.payError = '';

    this.api.payInvoice(
      this.selectedCard.id,
      this.invoiceToPay.year,
      this.invoiceToPay.month,
      {
        paymentMethod: Number(this.payMethod),
        paymentDate: new Date(this.payDate + 'T12:00:00Z').toISOString(),
        amount: Number(this.payAmount)
      }
    ).subscribe({
      next: () => {
        this.paying = false;
        this.closePayModal();
        this.loadCards();
      },
      error: err => {
        this.paying = false;
        this.payError = err.error?.message || 'Erro ao registrar pagamento da fatura.';
      }
    });
  }

  getLimitPercentage(card: CreditCard): number {
    if (!card.limit || card.limit <= 0) return 0;
    return Math.min(100, Math.round((card.totalUsedLimit / card.limit) * 100));
  }

  onEditTransaction(t: Transaction): void {
    this.editTransaction.emit(t);
  }

  onDeleteTransaction(t: Transaction): void {
    if (confirm(`Deseja realmente excluir o lançamento "${t.description}" de R$ ${Number(t.amount).toFixed(2)} desta fatura?`)) {
      this.api.deleteTransaction(t.id).subscribe();
    }
  }
}
