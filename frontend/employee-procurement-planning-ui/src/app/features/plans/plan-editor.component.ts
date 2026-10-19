import { Component, inject } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormArray, FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { Plan, Priority } from '../../core/models';

@Component({
  standalone: true,
  imports: [
    ReactiveFormsModule,
    FormsModule,
    RouterLink,
    CurrencyPipe,
    DatePipe,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './plan-editor.component.html'
})
export class PlanEditorComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);

  readonly priorities = [
    { value: 'Low', label: 'Low' },
    { value: 'Medium', label: 'Medium' },
    { value: 'High', label: 'High' },
    { value: 'Critical', label: 'Critical' }
  ];

  readonly form = this.formBuilder.nonNullable.group({
    title: ['', [Validators.required, Validators.maxLength(200)]],
    departmentId: ['', Validators.required],
    planningPeriodStart: ['', Validators.required],
    planningPeriodEnd: ['', Validators.required],
    priority: ['Medium', Validators.required],
    businessJustification: ['', [Validators.required, Validators.maxLength(4000)]],
    estimatedBudget: [0, [Validators.required, Validators.min(0)]],
    notes: ['', Validators.maxLength(4000)]
  });

  readonly items = new FormArray<FormGroup>([]);

  id?: string;
  planNumber = '';
  managerId: string | null = null;
  plan?: Plan;
  departments: Array<{ id: string; name: string; code: string }> = [];
  rowVersion = '';
  comment = '';
  loading = true;
  saving = false;
  actionInProgress = '';
  errorMessage = '';
  successMessage = '';

  get isNew(): boolean {
    return !this.id;
  }

  get editable(): boolean {
    return !this.plan || this.plan.status === 'Draft' || this.plan.status === 'ReturnedForCorrection';
  }

  get canReview(): boolean {
    return this.auth.hasRole('Manager') || this.auth.hasRole('Administrator');
  }

  get canSubmit(): boolean {
    return !!this.plan &&
      (this.plan.status === 'Draft' || this.plan.status === 'ReturnedForCorrection') &&
      this.plan.requesterId === this.auth.userId();
  }

  get canApprove(): boolean {
    return !!this.plan &&
      this.canReview &&
      (this.plan.status === 'Submitted' || this.plan.status === 'UnderReview');
  }

  get canReject(): boolean {
    return this.canApprove;
  }

  get canReturn(): boolean {
    return this.canApprove;
  }

  constructor() {
    this.loadDepartments();

    this.id = this.route.snapshot.paramMap.get('id') ?? undefined;

    if (this.id) {
      this.loadPlan(this.id);
    } else {
      this.loading = false;
      this.addItem();
    }
  }

  private loadDepartments(): void {
    this.api.departments().subscribe({
      next: departments => (this.departments = departments),
      error: () => (this.errorMessage = 'Departments could not be loaded.')
    });
  }

  private loadPlan(id: string): void {
    this.loading = true;
    this.api.plan(id).subscribe({
      next: plan => {
        this.plan = plan;
        this.planNumber = plan.planNumber;
        this.managerId = plan.managerId ?? null;
        this.rowVersion = plan.rowVersion;
        this.form.patchValue({
          title: plan.title,
          departmentId: plan.departmentId,
          planningPeriodStart: plan.planningPeriodStart,
          planningPeriodEnd: plan.planningPeriodEnd,
          priority: plan.priority,
          businessJustification: plan.businessJustification,
          estimatedBudget: plan.estimatedBudget,
          notes: plan.notes ?? ''
        });

        this.items.clear();
        for (const item of plan.items) {
          this.items.push(this.createItemGroup(item));
        }
        if (!this.items.length) {
          this.addItem();
        }
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.errorMessage = 'The procurement plan could not be loaded.';
      }
    });
  }

  private createItemGroup(item?: Plan['items'][number]) {
    return this.formBuilder.group({
      category: [item?.category ?? '', [Validators.required, Validators.maxLength(100)]],
      description: [item?.description ?? '', [Validators.required, Validators.maxLength(500)]],
      quantity: [item?.quantity ?? 1, [Validators.required, Validators.min(0.0001)]],
      estimatedUnitCost: [item?.estimatedUnitCost ?? 0, [Validators.required, Validators.min(0)]],
      requiredPeriod: [item?.requiredPeriod ?? '', Validators.required],
      priority: [item?.priority ?? 'Medium', Validators.required],
      justification: [item?.justification ?? '', [Validators.required, Validators.maxLength(2000)]],
      vendorPreference: [item?.vendorPreference ?? '', Validators.maxLength(200)]
    });
  }

  addItem(): void {
    this.items.push(this.createItemGroup());
  }

  removeItem(index: number): void {
    if (this.items.length === 1) {
      return;
    }
    this.items.removeAt(index);
  }

  save(): void {
    if (!this.editable || this.form.invalid || !this.items.length || this.items.invalid || this.saving) {
      this.form.markAllAsTouched();
      this.items.markAllAsTouched();
      return;
    }

    const values = this.form.getRawValue();
    const request = {
      ...values,
      managerId: this.managerId,
      requestedDateUtc: this.plan?.requestedDateUtc ?? new Date().toISOString(),
      items: this.items.getRawValue()
    };

    this.saving = true;
    this.errorMessage = '';
    this.successMessage = '';

    const operation = this.id
      ? this.api.update(this.id, { ...request, rowVersion: this.rowVersion })
      : this.api.create(request);

    operation
      .pipe(finalize(() => (this.saving = false)))
      .subscribe({
        next: plan => {
          this.plan = plan;
          this.id = plan.id;
          this.planNumber = plan.planNumber;
          this.rowVersion = plan.rowVersion;
          this.managerId = plan.managerId ?? null;
          this.successMessage = 'The draft was saved.';
          if (!this.route.snapshot.paramMap.get('id')) {
            void this.router.navigate(['/plans', plan.id], { replaceUrl: true });
          }
        },
        error: error => {
          this.errorMessage = error.error?.detail ?? error.error?.message ?? 'The plan could not be saved.';
        }
      });
  }

  transition(action: 'submit' | 'review' | 'approve' | 'reject' | 'return' | 'cancel'): void {
    if (!this.id || this.actionInProgress) {
      return;
    }

    if ((action === 'reject' || action === 'return') && !this.comment.trim()) {
      this.errorMessage = 'A comment is required for this action.';
      return;
    }

    this.actionInProgress = action;
    this.errorMessage = '';
    this.successMessage = '';

    this.api.transition(this.id, action, this.rowVersion, this.comment.trim() || undefined)
      .pipe(finalize(() => (this.actionInProgress = '')))
      .subscribe({
        next: plan => {
          this.plan = plan;
          this.rowVersion = plan.rowVersion;
          this.comment = '';
          this.successMessage = `Plan ${plan.status.replaceAll(/([a-z])([A-Z])/g, '$1 $2').toLowerCase()} successfully.`;
        },
        error: error => {
          this.errorMessage = error.error?.detail ?? error.error?.message ?? 'The workflow action could not be completed.';
        }
      });
  }

  priorityLabel(priority: Priority): string {
    return priority;
  }
}
