import { Component, inject } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ApiService } from '../../core/api.service';
import { Dashboard } from '../../core/models';

@Component({
  standalone: true,
  imports: [CurrencyPipe, DatePipe, MatProgressSpinnerModule],
  templateUrl: './dashboard.component.html'
})
export class DashboardComponent {
  private readonly api = inject(ApiService);

  data?: Dashboard;
  loading = true;
  errorMessage = '';

  constructor() {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.errorMessage = '';
    this.api.dashboard().subscribe({
      next: data => {
        this.data = data;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.errorMessage = 'The dashboard could not be loaded.';
      }
    });
  }
}
