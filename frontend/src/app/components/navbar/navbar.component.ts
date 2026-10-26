import { Component, EventEmitter, Output } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { User } from '../../models/models';

@Component({
  selector: 'app-navbar',
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.css']
})
export class NavbarComponent {
  currentUser: User | null = null;
  @Output() openNewTransaction = new EventEmitter<void>();
  @Output() openBalanceModal = new EventEmitter<void>();
  @Output() openResetModal = new EventEmitter<void>();

  constructor(public authService: AuthService, private router: Router) {
    this.authService.currentUser$.subscribe(user => {
      this.currentUser = user;
    });
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
