export enum TransactionType {
  Entrada = 1,
  SaidaFixa = 2,
  Diario = 3
}

export enum TransactionStatus {
  Realizado = 1,
  Previsto = 2
}

export enum PaymentMethod {
  Pix = 1,
  CartaoCredito = 2,
  CartaoDebito = 3,
  Dinheiro = 4,
  Boleto = 5,
  Transferencia = 6,
  Outro = 7
}

export interface User {
  id: number;
  name: string;
  email: string;
  initialBalance: number;
  createdAt: string;
}

export interface AuthResponse {
  token: string;
  user: User;
}

export interface Category {
  id: number;
  name: string;
  type: TransactionType;
  typeName?: string;
  colorHex: string;
  icon: string;
  isDefault: boolean;
}

export interface CreditCard {
  id: number;
  name: string;
  limit: number;
  closingDay: number;
  dueDay: number;
  colorHex: string;
  brand: string;
  isActive: boolean;
  totalUsedLimit: number;
  availableLimit: number;
  currentInvoiceTotal: number;
  createdAt: string;
}

export interface CreditCardCreateUpdate {
  name: string;
  limit: number;
  closingDay: number;
  dueDay: number;
  colorHex?: string;
  brand?: string;
  isActive?: boolean;
}

export interface CreditCardInvoiceSummary {
  creditCardId: number;
  creditCardName: string;
  creditCardColor: string;
  brand: string;
  year: number;
  month: number;
  monthName: string;
  dueDate: string;
  closingDate: string;
  totalAmount: number;
  isPaid: boolean;
  status: string; // "Aberta" | "Fechada" | "Paga"
  transactions: Transaction[];
}

export interface PayInvoiceRequest {
  paymentMethod: PaymentMethod;
  paymentDate?: string;
  amount?: number;
}

export interface Transaction {
  id: number;
  date: string;
  day: number;
  month: number;
  year: number;
  type: TransactionType;
  typeName: string;
  amount: number;
  description: string;
  categoryId?: number;
  categoryName: string;
  categoryColor: string;
  categoryIcon: string;
  status: TransactionStatus;
  statusName: string;
  paymentMethod: PaymentMethod;
  paymentMethodName: string;
  notes?: string;
  creditCardId?: number;
  creditCardName?: string;
  installmentNumber?: number;
  totalInstallments?: number;
  invoiceMonth?: number;
  invoiceYear?: number;
  isInvoicePayment?: boolean;
  recurrenceGroupId?: string;
  isRecurring?: boolean;
  createdAt: string;
}

export interface TransactionCreateUpdate {
  date: string;
  type: TransactionType;
  amount: number;
  description: string;
  categoryId?: number;
  status: TransactionStatus;
  paymentMethod: PaymentMethod;
  notes?: string;
  creditCardId?: number;
  installments?: number;
  isRecurring?: boolean;
  recurrenceMonths?: number;
}

export interface DailyFlow {
  day: number;
  date: string;
  dayOfWeek: string;
  totalIncome: number;
  totalFixedExpenses: number;
  totalDailyExpenses: number;
  totalExpenses: number;
  netDay: number;
  cumulativeBalance: number;
  transactionsCount: number;
  transactions: Transaction[];
  expanded?: boolean;
}

export interface MonthlyFlowResponse {
  year: number;
  month: number;
  monthName: string;
  initialBalance: number;
  totalIncome: number;
  totalFixedExpenses: number;
  totalDailyExpenses: number;
  totalExpenses: number;
  performance: number;
  finalBalance: number;
  savingsRate: number;
  days: DailyFlow[];
}

export interface AnnualMonthSummary {
  month: number;
  monthName: string;
  totalIncome: number;
  totalExpenses: number;
  performance: number;
  savingsRate: number;
  endOfMonthBalance: number;
}

export interface AnnualEconomyResponse {
  year: number;
  initialBalanceOfYear: number;
  totalIncome: number;
  totalExpenses: number;
  totalPerformance: number;
  overallSavingsRate: number;
  endOfYearBalance: number;
  months: AnnualMonthSummary[];
}

export interface DailyTrendPoint {
  day: number;
  label: string;
  balance: number;
  income: number;
  expense: number;
}

export interface CategoryExpenseShare {
  categoryName: string;
  colorHex: string;
  totalAmount: number;
  percentage: number;
}

export interface DashboardSummary {
  year: number;
  month: number;
  monthName: string;
  currentBalance: number;
  initialBalance: number;
  monthIncome: number;
  monthFixedExpenses: number;
  monthDailyExpenses: number;
  monthTotalExpenses: number;
  monthCreditCardTotal?: number;
  monthPerformance: number;
  monthSavingsRate: number;
  recentTransactions: Transaction[];
  dailyTrend: DailyTrendPoint[];
  categoryDistribution: CategoryExpenseShare[];
}
