export type Role = 'Administrator' | 'Manager' | 'Employee';

export type PlanStatus =
  | 'Draft'
  | 'Submitted'
  | 'UnderReview'
  | 'Approved'
  | 'Rejected'
  | 'ReturnedForCorrection'
  | 'Cancelled';

export type Priority = 'Low' | 'Medium' | 'High' | 'Critical';

export interface PlanItem {
  id: string;
  category: string;
  description: string;
  quantity: number;
  estimatedUnitCost: number;
  requiredPeriod: string;
  priority: Priority;
  justification: string;
  vendorPreference?: string;
  status: 'Draft' | 'Requested' | 'Approved' | 'Rejected';
  estimatedTotalCost: number;
}

export interface AuditEntry {
  id: string;
  action: string;
  previousStatus?: string;
  newStatus?: string;
  comment?: string;
  createdAtUtc: string;
  actorName?: string;
}

export interface Plan {
  id: string;
  planNumber: string;
  title: string;
  departmentId: string;
  requesterId: string;
  managerId?: string;
  planningPeriodStart: string;
  planningPeriodEnd: string;
  priority: Priority;
  status: PlanStatus;
  businessJustification: string;
  estimatedBudget: number;
  requestedDateUtc: string;
  notes?: string;
  rowVersion: string;
  items: PlanItem[];
  history: AuditEntry[];
}

export interface Paged<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface Dashboard {
  totalPlans: number;
  draftPlans: number;
  submittedPlans: number;
  awaitingApproval: number;
  approvedPlans: number;
  rejectedReturnedPlans: number;
  estimatedTotalBudget: number;
  trends: Array<{ label: string; count: number; budget: number }>;
  recentActivity: AuditEntry[];
}

export interface User {
  id: string;
  email: string;
  displayName: string;
  departmentName?: string;
  roles: Role[];
  isActive: boolean;
  createdAtUtc: string;
}

export interface LoginResponse {
  accessToken: string;
  expiresAtUtc: string;
  displayName: string;
  roles: Role[];
}
