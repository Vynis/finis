import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, Subject, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  AnnualEconomyResponse,
  Category,
  CreditCard,
  CreditCardCreateUpdate,
  CreditCardInvoiceSummary,
  DashboardSummary,
  MonthlyFlowResponse,
  PayInvoiceRequest,
  Transaction,
  TransactionCreateUpdate
} from '../models/models';

@Injectable({
  providedIn: 'root'
})
export class FinisApiService {
  private apiUrl = environment.apiUrl;

  // Reactive stream to automatically refresh UI components across the app
  private refreshSubject = new Subject<void>();
  public refresh$ = this.refreshSubject.asObservable();

  constructor(private http: HttpClient) {}

  notifyDataChanged(): void {
    this.refreshSubject.next();
  }

  // Dashboard
  getDashboardSummary(year?: number, month?: number): Observable<DashboardSummary> {
    let params = new HttpParams();
    if (year) params = params.set('year', year.toString());
    if (month) params = params.set('month', month.toString());
    return this.http.get<DashboardSummary>(`${this.apiUrl}/dashboard`, { params });
  }

  // Monthly Flow (Termômetro Diário)
  getMonthlyFlow(year: number, month: number): Observable<MonthlyFlowResponse> {
    return this.http.get<MonthlyFlowResponse>(`${this.apiUrl}/monthlyflow/${year}/${month}`);
  }

  // Annual Economy
  getAnnualEconomy(year: number): Observable<AnnualEconomyResponse> {
    return this.http.get<AnnualEconomyResponse>(`${this.apiUrl}/economy/${year}`);
  }

  // Transactions
  getTransactions(filter?: { year?: number; month?: number; type?: number; categoryId?: number; creditCardId?: number }): Observable<Transaction[]> {
    let params = new HttpParams();
    if (filter?.year) params = params.set('year', filter.year.toString());
    if (filter?.month) params = params.set('month', filter.month.toString());
    if (filter?.type) params = params.set('type', filter.type.toString());
    if (filter?.categoryId) params = params.set('categoryId', filter.categoryId.toString());
    if (filter?.creditCardId) params = params.set('creditCardId', filter.creditCardId.toString());
    return this.http.get<Transaction[]>(`${this.apiUrl}/transactions`, { params });
  }

  createTransaction(dto: TransactionCreateUpdate): Observable<Transaction> {
    return this.http.post<Transaction>(`${this.apiUrl}/transactions`, dto).pipe(
      tap(() => this.notifyDataChanged())
    );
  }

  updateTransaction(id: number, dto: TransactionCreateUpdate): Observable<Transaction> {
    return this.http.put<Transaction>(`${this.apiUrl}/transactions/${id}`, dto).pipe(
      tap(() => this.notifyDataChanged())
    );
  }

  deleteTransaction(id: number, options?: { deleteFuture?: boolean; deleteAllSeries?: boolean }): Observable<void> {
    let params = new HttpParams();
    if (options?.deleteFuture) params = params.set('deleteFuture', 'true');
    if (options?.deleteAllSeries) params = params.set('deleteAllSeries', 'true');
    return this.http.delete<void>(`${this.apiUrl}/transactions/${id}`, { params }).pipe(
      tap(() => this.notifyDataChanged())
    );
  }

  // Categories
  getCategories(): Observable<Category[]> {
    return this.http.get<Category[]>(`${this.apiUrl}/categories`);
  }

  createCategory(category: Partial<Category>): Observable<Category> {
    return this.http.post<Category>(`${this.apiUrl}/categories`, category).pipe(
      tap(() => this.notifyDataChanged())
    );
  }

  updateCategory(id: number, category: Partial<Category>): Observable<Category> {
    return this.http.put<Category>(`${this.apiUrl}/categories/${id}`, category).pipe(
      tap(() => this.notifyDataChanged())
    );
  }

  deleteCategory(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/categories/${id}`).pipe(
      tap(() => this.notifyDataChanged())
    );
  }

  // Credit Cards
  getCreditCards(): Observable<CreditCard[]> {
    return this.http.get<CreditCard[]>(`${this.apiUrl}/creditcards`);
  }

  getCreditCardById(id: number): Observable<CreditCard> {
    return this.http.get<CreditCard>(`${this.apiUrl}/creditcards/${id}`);
  }

  createCreditCard(dto: CreditCardCreateUpdate): Observable<CreditCard> {
    return this.http.post<CreditCard>(`${this.apiUrl}/creditcards`, dto).pipe(
      tap(() => this.notifyDataChanged())
    );
  }

  updateCreditCard(id: number, dto: CreditCardCreateUpdate): Observable<CreditCard> {
    return this.http.put<CreditCard>(`${this.apiUrl}/creditcards/${id}`, dto).pipe(
      tap(() => this.notifyDataChanged())
    );
  }

  deleteCreditCard(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/creditcards/${id}`).pipe(
      tap(() => this.notifyDataChanged())
    );
  }

  getInvoice(cardId: number, year: number, month: number): Observable<CreditCardInvoiceSummary> {
    return this.http.get<CreditCardInvoiceSummary>(`${this.apiUrl}/creditcards/${cardId}/invoices/${year}/${month}`);
  }

  getUpcomingInvoices(cardId: number, count = 6): Observable<CreditCardInvoiceSummary[]> {
    return this.http.get<CreditCardInvoiceSummary[]>(`${this.apiUrl}/creditcards/${cardId}/invoices/upcoming?count=${count}`);
  }

  payInvoice(cardId: number, year: number, month: number, req: PayInvoiceRequest): Observable<Transaction> {
    return this.http.post<Transaction>(`${this.apiUrl}/creditcards/${cardId}/invoices/${year}/${month}/pay`, req).pipe(
      tap(() => this.notifyDataChanged())
    );
  }
}
