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

export enum AccountType {
  Pessoal = 1,
  Empresa = 2,
  Investimento = 3,
  Outro = 4
}

export interface Account {
  id: number;
  name: string;
  type: AccountType;
  typeName: string;
  initialBalance: number;
  currentBalance: number;
  colorHex: string;
  icon: string;
  isActive: boolean;
  transactionsCount: number;
  createdAt: string;
}

export interface AccountCreateUpdate {
  name: string;
  type: AccountType;
  initialBalance: number;
  colorHex?: string;
  icon?: string;
  isActive?: boolean;
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
  accountId?: number;
  accountName?: string;
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
  accountId?: number;
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
  accountId?: number;
  accountName?: string;
  accountColor?: string;
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
  accountId?: number;
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

export interface OverdueSummary {
  totalCount: number;
  totalExpenses: number;
  totalIncome: number;
  transactions: Transaction[];
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
  realizedIncome: number;
  pendingIncome: number;
  realizedExpenses: number;
  pendingExpenses: number;
  realizedPerformance: number;
  pendingPerformance: number;
  overdueSummary: OverdueSummary;
  recentTransactions: Transaction[];
  dailyTrend: DailyTrendPoint[];
  categoryDistribution: CategoryExpenseShare[];
}

export interface Goal {
  id: number;
  name: string;
  accountId?: number;
  accountName?: string;
  targetAmount: number;
  currentAmount: number;
  remainingAmount: number;
  percentageCompleted: number;
  targetDate?: string;
  category: string;
  colorHex: string;
  icon: string;
  priority: number;
  priorityLabel: string;
  isCompleted: boolean;
  monthlyRequiredSavings: number;
  monthsRemaining?: number;
  createdAt: string;
}

export interface GoalCreateUpdate {
  name: string;
  accountId?: number;
  targetAmount: number;
  currentAmount?: number;
  targetDate?: string;
  category?: string;
  colorHex?: string;
  icon?: string;
  priority?: number;
  isCompleted?: boolean;
}

export interface Debt {
  id: number;
  name: string;
  creditor?: string;
  accountId?: number;
  accountName?: string;
  totalAmount: number;
  remainingAmount: number;
  paidAmount: number;
  percentagePaid: number;
  interestRateMonthly: number;
  monthlyPayment: number;
  dueDay: number;
  colorHex: string;
  notes?: string;
  isSettled: boolean;
  isHighInterest: boolean;
  estimatedMonthsToPayoff?: number;
  monthlyInterestCostEstimate: number;
  createdAt: string;
}

export interface DebtCreateUpdate {
  name: string;
  creditor?: string;
  accountId?: number;
  totalAmount: number;
  remainingAmount: number;
  interestRateMonthly: number;
  monthlyPayment: number;
  dueDay: number;
  colorHex?: string;
  notes?: string;
  isSettled?: boolean;
}

export interface DebtRecommendation {
  debtId: number;
  debtName: string;
  creditor?: string;
  remainingAmount: number;
  interestRateMonthly: number;
  suggestedAmortization: number;
  estimatedInterestSavedMonthly: number;
  urgencyLabel: string;
  strategyReason: string;
}

export interface GoalRecommendation {
  goalId: number;
  goalName: string;
  targetAmount: number;
  currentAmount: number;
  suggestedDeposit: number;
  monthsAhead?: number;
  strategyReason: string;
}

export interface AdvisorAnalysis {
  year: number;
  month: number;
  monthName: string;
  projectedSurplus: number;
  currentRealizedSurplus: number;
  dailySafeSpendingTarget: number;
  daysRemainingInMonth: number;
  healthStatus: string;
  healthBadgeClass: string;
  primaryRecommendationTitle: string;
  primaryRecommendationMessage: string;
  totalDebtsRemaining: number;
  totalMonthlyInterestCost: number;
  totalGoalsTarget: number;
  totalGoalsCurrent: number;
  debtActions: DebtRecommendation[];
  goalActions: GoalRecommendation[];
  smartTips: string[];
}
