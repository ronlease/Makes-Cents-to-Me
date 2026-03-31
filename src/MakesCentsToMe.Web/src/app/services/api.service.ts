import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map, Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export type AccountType = 'Checking' | 'CreditCard' | 'MoneyMarket' | 'Savings';

export type AmountType = 'Single' | 'Split';

export interface Account {
  accountType: AccountType;
  hasImportProfile: boolean;
  id: string;
  name: string;
}

export interface AccountCreateRequest {
  accountType: AccountType;
  name: string;
}

export interface AccountUpdateRequest {
  accountType: AccountType;
  name: string;
}

export interface Category {
  id: string;
  isDefault: boolean;
  name: string;
  transactionCount: number;
}

export interface CategoryCreateRequest {
  name: string;
}

export interface CategoryUpdateRequest {
  name: string;
}

export interface ApiResponse<T> {
  data: T;
  errors: string[];
  success: boolean;
}

export interface ColumnMapping {
  applicationField: string;
  csvColumnName: string;
  id: string;
}

export interface ColumnMappingRequest {
  applicationField: string;
  csvColumnName: string;
}

export interface ImportProfile {
  accountId: string;
  amountType: AmountType;
  balanceProvided: boolean;
  columnMappings: ColumnMapping[];
  dateFormat: string;
  id: string;
}

export interface ImportProfileRequest {
  amountType: AmountType;
  balanceProvided: boolean;
  columnMappings: ColumnMappingRequest[];
  dateFormat: string;
}

export interface ImportProcessRequest {
  closingBalance?: number;
  openingBalance?: number;
}

export interface ImportResult {
  duplicatesSkipped: number;
  rowsSkipped: number;
  transactionsCreated: number;
}

export interface Institution {
  accountCount: number;
  id: string;
  name: string;
}

export interface InstitutionCreateRequest {
  name: string;
}

export interface InstitutionUpdateRequest {
  name: string;
}

export interface OverrideTransactionRequest {
  categoryId: string | null;
  normalizedVendor: string;
}

export interface ReviewTransaction {
  accountName: string;
  amount: number;
  categoryId: string | null;
  confidence: number | null;
  date: string;
  description: string;
  id: string;
  institutionName: string;
  normalizedVendor: string | null;
  rawCategory: string | null;
  status: string;
  suggestedCategory: string | null;
  suggestedCategoryId: string | null;
  suggestedNormalizedVendor: string | null;
}

export interface UploadPreviewResponse {
  headers: string[];
  previewRows: string[][];
}

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly baseUrl = environment.apiUrl;
  private readonly http = inject(HttpClient);

  // Account endpoints
  createAccount(institutionId: string, request: AccountCreateRequest): Observable<Account> {
    return this.http
      .post<ApiResponse<Account>>(`${this.baseUrl}/api/v1/institutions/${institutionId}/accounts`, request)
      .pipe(map(response => response.data));
  }

  deleteAccount(institutionId: string, accountId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/api/v1/institutions/${institutionId}/accounts/${accountId}`);
  }

  getAccounts(institutionId: string): Observable<Account[]> {
    return this.http
      .get<ApiResponse<Account[]>>(`${this.baseUrl}/api/v1/institutions/${institutionId}/accounts`)
      .pipe(map(response => response.data ?? []));
  }

  updateAccount(institutionId: string, accountId: string, request: AccountUpdateRequest): Observable<Account> {
    return this.http
      .put<ApiResponse<Account>>(`${this.baseUrl}/api/v1/institutions/${institutionId}/accounts/${accountId}`, request)
      .pipe(map(response => response.data));
  }

  // Category endpoints
  createCategory(request: CategoryCreateRequest): Observable<Category> {
    return this.http
      .post<ApiResponse<Category>>(`${this.baseUrl}/api/v1/categories`, request)
      .pipe(map(response => response.data));
  }

  deleteCategory(categoryId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/api/v1/categories/${categoryId}`);
  }

  getCategories(): Observable<Category[]> {
    return this.http
      .get<ApiResponse<Category[]>>(`${this.baseUrl}/api/v1/categories`)
      .pipe(map(response => response.data ?? []));
  }

  updateCategory(categoryId: string, request: CategoryUpdateRequest): Observable<Category> {
    return this.http
      .put<ApiResponse<Category>>(`${this.baseUrl}/api/v1/categories/${categoryId}`, request)
      .pipe(map(response => response.data));
  }

  // Import endpoints
  deleteImportProfile(accountId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/api/v1/accounts/${accountId}/import/profile`);
  }

  getImportProfile(accountId: string): Observable<ImportProfile> {
    return this.http
      .get<ApiResponse<ImportProfile>>(`${this.baseUrl}/api/v1/accounts/${accountId}/import/profile`)
      .pipe(map(response => response.data));
  }

  processImport(accountId: string, file: File, request: ImportProcessRequest): Observable<ImportResult> {
    const formData = new FormData();
    formData.append('file', file);
    if (request.openingBalance !== undefined) {
      formData.append('openingBalance', String(request.openingBalance));
    }
    if (request.closingBalance !== undefined) {
      formData.append('closingBalance', String(request.closingBalance));
    }
    return this.http
      .post<ApiResponse<ImportResult>>(`${this.baseUrl}/api/v1/accounts/${accountId}/import/process`, formData)
      .pipe(map(response => response.data));
  }

  saveImportProfile(accountId: string, request: ImportProfileRequest): Observable<ImportProfile> {
    return this.http
      .post<ApiResponse<ImportProfile>>(`${this.baseUrl}/api/v1/accounts/${accountId}/import/profile`, request)
      .pipe(map(response => response.data));
  }

  updateImportProfile(accountId: string, request: ImportProfileRequest): Observable<ImportProfile> {
    return this.http
      .put<ApiResponse<ImportProfile>>(`${this.baseUrl}/api/v1/accounts/${accountId}/import/profile`, request)
      .pipe(map(response => response.data));
  }

  uploadCsv(accountId: string, file: File): Observable<UploadPreviewResponse> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http
      .post<ApiResponse<UploadPreviewResponse>>(`${this.baseUrl}/api/v1/accounts/${accountId}/import/upload`, formData)
      .pipe(map(response => response.data));
  }

  // Review queue endpoints
  acceptAllTransactions(accountId?: string): Observable<number> {
    const params = accountId ? `?accountId=${accountId}` : '';
    return this.http
      .post<ApiResponse<number>>(`${this.baseUrl}/api/v1/review/accept-all${params}`, {})
      .pipe(map(response => response.data));
  }

  acceptTransaction(transactionId: string): Observable<ReviewTransaction> {
    return this.http
      .put<ApiResponse<ReviewTransaction>>(`${this.baseUrl}/api/v1/review/${transactionId}/accept`, {})
      .pipe(map(response => response.data));
  }

  getReviewQueue(accountId?: string): Observable<ReviewTransaction[]> {
    const params = accountId ? `?accountId=${accountId}` : '';
    return this.http
      .get<ApiResponse<ReviewTransaction[]>>(`${this.baseUrl}/api/v1/review${params}`)
      .pipe(map(response => response.data ?? []));
  }

  overrideTransaction(transactionId: string, request: OverrideTransactionRequest): Observable<ReviewTransaction> {
    return this.http
      .put<ApiResponse<ReviewTransaction>>(`${this.baseUrl}/api/v1/review/${transactionId}/override`, request)
      .pipe(map(response => response.data));
  }

  // Institution endpoints
  createInstitution(request: InstitutionCreateRequest): Observable<Institution> {
    return this.http
      .post<ApiResponse<Institution>>(`${this.baseUrl}/api/v1/institutions`, request)
      .pipe(map(response => response.data));
  }

  deleteInstitution(institutionId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/api/v1/institutions/${institutionId}`);
  }

  getInstitution(institutionId: string): Observable<Institution> {
    return this.http
      .get<ApiResponse<Institution>>(`${this.baseUrl}/api/v1/institutions/${institutionId}`)
      .pipe(map(response => response.data));
  }

  getInstitutions(): Observable<Institution[]> {
    return this.http
      .get<ApiResponse<Institution[]>>(`${this.baseUrl}/api/v1/institutions`)
      .pipe(map(response => response.data ?? []));
  }

  updateInstitution(institutionId: string, request: InstitutionUpdateRequest): Observable<Institution> {
    return this.http
      .put<ApiResponse<Institution>>(`${this.baseUrl}/api/v1/institutions/${institutionId}`, request)
      .pipe(map(response => response.data));
  }
}
