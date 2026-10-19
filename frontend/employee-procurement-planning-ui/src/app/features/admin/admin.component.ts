import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { ApiService } from '../../core/api.service';
import { User } from '../../core/models';

@Component({
  standalone: true,
  imports: [FormsModule, MatTableModule, MatButtonModule, MatSelectModule],
  templateUrl: './admin.component.html'
})
export class AdminComponent {
  private readonly api = inject(ApiService);

  users: User[] = [];
  readonly columns = ['user', 'department', 'role', 'status', 'actions'];
  loading = true;
  savingUserId: string | null = null;
  errorMessage = '';
  successMessage = '';

  constructor() {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.errorMessage = '';
    this.api.users().subscribe({
      next: users => {
        this.users = users;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.errorMessage = 'The user list could not be loaded.';
      }
    });
  }

  toggle(user: User): void {
    user.isActive = !user.isActive;
    this.successMessage = '';
    this.errorMessage = '';
  }

  save(user: User): void {
    this.savingUserId = user.id;
    this.errorMessage = '';
    this.successMessage = '';

    this.api.updateUser(user.id, {
      roles: user.roles,
      isActive: user.isActive
    }).subscribe({
      next: () => {
        this.savingUserId = null;
        this.successMessage = `${user.displayName}'s account was updated.`;
      },
      error: error => {
        this.savingUserId = null;
        this.errorMessage = error.error?.message ?? 'The account could not be updated.';
        this.load();
      }
    });
  }
}
