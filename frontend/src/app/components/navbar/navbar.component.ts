import { Component, EventEmitter, OnDestroy, OnInit, Output } from '@angular/core';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { AuthService } from '../../services/auth.service';
import { FinisApiService } from '../../services/finis-api.service';
import { Account, User } from '../../models/models';

@Component({
  selector: 'app-navbar',
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.css']
})
export class NavbarComponent implements OnInit, OnDestroy {
  currentUser: User | null = null;
  accounts: Account[] = [];
  selectedAccountId: number | null = null;
  selectedAccount: Account | null = null;

  @Output() openNewTransaction = new EventEmitter<void>();
  @Output() openBalanceModal = new EventEmitter<void>();
  @Output() openResetModal = new EventEmitter<void>();

  private subs = new Subscription();

  constructor(
    public authService: AuthService,
    private api: FinisApiService,
    private router: Router
  ) {
    this.subs.add(
      this.authService.currentUser$.subscribe(user => {
        this.currentUser = user;
        if (user) {
          this.loadAccounts();
        }
      })
    );

    this.subs.add(
      this.api.selectedAccountId$.subscribe(id => {
        this.selectedAccountId = id;
        this.updateSelectedAccount();
      })
    );

    this.subs.add(
      this.api.refresh$.subscribe(() => {
        if (this.currentUser) {
          this.loadAccounts();
        }
      })
    );
  }

  ngOnInit(): void {
    if (this.currentUser) {
      this.loadAccounts();
    }
  }

  ngOnDestroy(): void {
    this.subs.unsubscribe();
  }

  loadAccounts(): void {
    this.api.getAccounts().subscribe({
      next: accs => {
        this.accounts = accs;
        this.updateSelectedAccount();
      }
    });
  }

  updateSelectedAccount(): void {
    if (this.selectedAccountId !== null && this.accounts.length > 0) {
      this.selectedAccount = this.accounts.find(a => a.id === this.selectedAccountId) || null;
    } else {
      this.selectedAccount = null;
    }
  }

  selectAccount(id: number | null): void {
    this.api.setSelectedAccountId(id);
  }

  onNewTransaction(): void {
    this.openNewTransaction.emit();
  }

  onEditBalance(): void {
    this.openBalanceModal.emit();
  }

  onResetAll(): void {
    this.openResetModal.emit();
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
