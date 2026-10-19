import { Component, inject } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { Plan, PlanStatus, Priority } from '../../core/models';

@Component({
  standalone: true,
  imports: [
    CurrencyPipe,
    RouterLink,
    FormsModule,
    MatTableModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule
  ],
  templateUrl: './plans.component.html'
})
export class PlansComponent {
  private readonly api = inject(ApiService);

  plans: Plan[] = [];
  readonly columns = ['number', 'status', 'priority', 'budget', 'actions'];
  readonly statuses: Array<{ value: PlanStatus | ''; label: string }> = [
    { value: '', label: 'All statuses' },
    { value: 'Draft', label: 'Draft' },
    { value: 'Submitted', label: 'Submitted' },
    { value: 'UnderReview', label: 'Under review' },
    { value: 'Approved', label: 'Approved' },
    { value: 'Rejected', label: 'Rejected' },
    { value: 'ReturnedForCorrection', label: 'Returned for correction' },
    { value: 'Cancelled', label: 'Cancelled' }
  ];
  readonly priorities = [
    { value: '', label: 'All priorities' },
    { value: 'Low', label: 'Low' },
    { value: 'Medium', label: 'Medium' },
    { value: 'High', label: 'High' },
    { value: 'Critical', label: 'Critical' }
  ];

  search = '';
  status: PlanStatus | '' = '';
  priority: Priority | '' = '';
  page = 1;
  readonly pageSize = 20;
  total = 0;
  loading = true;
  errorMessage = '';

  constructor() {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.errorMessage = '';

    this.api.plans({
      search: this.search.trim(),
      status: this.status || undefined,
      priority: this.priority === '' ? undefined : this.priority,
      page: this.page,
      pageSize: this.pageSize
    }).subscribe({
      next: result => {
        this.plans = result.items;
        this.total = result.totalCount;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.errorMessage = 'The procurement plans could not be loaded.';
      }
    });
  }

  resetFilters(): void {
    this.search = '';
    this.status = '';
    this.priority = '';
    this.page = 1;
    this.load();
  }

  previousPage(): void {
    if (this.page <= 1) {
      return;
    }
    this.page--;
    this.load();
  }

  nextPage(): void {
    if (this.page * this.pageSize >= this.total) {
      return;
    }
    this.page++;
    this.load();
  }

  get totalPages(): number {
    return this.total ? Math.ceil(this.total / this.pageSize) : 1;
  }

  priorityLabel(priority: Priority): string {
    return priority;
  }
}
