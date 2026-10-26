import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthResponse, User } from '../models/models';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = `${environment.apiUrl}/auth`;
  private currentUserSubject: BehaviorSubject<User | null>;
  public currentUser$: Observable<User | null>;

  constructor(private http: HttpClient) {
    const savedUser = localStorage.getItem('finis_user');
    this.currentUserSubject = new BehaviorSubject<User | null>(
      savedUser ? JSON.parse(savedUser) : null
    );
    this.currentUser$ = this.currentUserSubject.asObservable();
  }

  public get currentUserValue(): User | null {
    return this.currentUserSubject.value;
  }

  public getToken(): string | null {
    return localStorage.getItem('finis_token');
  }

  public isAuthenticated(): boolean {
    return !!this.getToken();
  }

  login(credentials: { email: string; password: string }): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, credentials).pipe(
      tap(res => {
        if (res && res.token) {
          localStorage.setItem('finis_token', res.token);
          localStorage.setItem('finis_user', JSON.stringify(res.user));
          this.currentUserSubject.next(res.user);
        }
      })
    );
  }

  register(data: { name: string; email: string; password: string; initialBalance: number }): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/register`, data).pipe(
      tap(res => {
        if (res && res.token) {
          localStorage.setItem('finis_token', res.token);
          localStorage.setItem('finis_user', JSON.stringify(res.user));
          this.currentUserSubject.next(res.user);
        }
      })
    );
  }

  logout(): void {
    localStorage.removeItem('finis_token');
    localStorage.removeItem('finis_user');
    this.currentUserSubject.next(null);
  }

  getProfile(): Observable<User> {
    return this.http.get<User>(`${this.apiUrl}/me`).pipe(
      tap(user => {
        localStorage.setItem('finis_user', JSON.stringify(user));
        this.currentUserSubject.next(user);
      })
    );
  }

  updateInitialBalance(initialBalance: number): Observable<User> {
    return this.http.put<User>(`${this.apiUrl}/initial-balance`, { initialBalance }).pipe(
      tap(user => {
        localStorage.setItem('finis_user', JSON.stringify(user));
        this.currentUserSubject.next(user);
      })
    );
  }

  resetAllUserData(): Observable<User> {
    return this.http.post<User>(`${this.apiUrl}/reset-data`, {}).pipe(
      tap(user => {
        localStorage.setItem('finis_user', JSON.stringify(user));
        this.currentUserSubject.next(user);
      })
    );
  }
}
