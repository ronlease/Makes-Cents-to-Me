import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export type AccountType = 'Checking' | 'CreditCard' | 'MoneyMarket' | 'Savings';

export interface Account {
  accountId: number;
  accountType: AccountType;
  hasImportProfile: boolean;
  institutionId: number;
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

export interface CsvPreview {
  detectedHeaders: string[];
  previewRows: string[][];
}

export interface ImportProfile {
  accountId: number;
  amountType: 'Single' | 'Split';
  balanceProvided: boolean;
  columnMappings: Record<string, string>;
  dateFormat: string;
}

export interface ImportProfileRequest {
  amountType: 'Single' | 'Split';
  balanceProvided: boolean;
  columnMappings: Record<string, string>;
  dateFormat: string;
}

export interface ImportProcessRequest {
  closingBalance?: number;
  openingBalance?: number;
}

export interface ImportResult {
  duplicatesSkipped: number;
  transactionsCreated: number;
}

export interface Institution {
  accountCount: number;
  institutionId: number;
  name: string;
}

export interface InstitutionCreateRequest {
  name: string;
}

export interface InstitutionUpdateRequest {
  name: string;
}

export interface UploadResponse {
  importSessionId: string;
  preview: CsvPreview;
}

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly baseUrl = environment.apiUrl;
  private readonly http = inject(HttpClient);

  // Account endpoints
  createAccount(institutionId: number, request: AccountCreateRequest): Observable<Account> {
    return this.http.post<Account>(`${this.baseUrl}/api/v1/institutions/${institutionId}/accounts`, request);
  }

  deleteAccount(institutionId: number, accountId: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/api/v1/institutions/${institutionId}/accounts/${accountId}`);
  }

  getAccounts(institutionId: number): Observable<Account[]> {
    return this.http.get<Account[]>(`${this.baseUrl}/api/v1/institutions/${institutionId}/accounts`);
  }

  updateAccount(institutionId: number, accountId: number, request: AccountUpdateRequest): Observable<Account> {
    return this.http.put<Account>(`${this.baseUrl}/api/v1/institutions/${institutionId}/accounts/${accountId}`, request);
  }

  // Import endpoints
  deleteImportProfile(accountId: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/api/v1/accounts/${accountId}/import/profile`);
  }

  getImportProfile(accountId: number): Observable<ImportProfile> {
    return this.http.get<ImportProfile>(`${this.baseUrl}/api/v1/accounts/${accountId}/import/profile`);
  }

  processImport(accountId: number, request: ImportProcessRequest): Observable<ImportResult> {
    return this.http.post<ImportResult>(`${this.baseUrl}/api/v1/accounts/${accountId}/import/process`, request);
  }

  saveImportProfile(accountId: number, request: ImportProfileRequest): Observable<ImportProfile> {
    return this.http.post<ImportProfile>(`${this.baseUrl}/api/v1/accounts/${accountId}/import/profile`, request);
  }

  updateImportProfile(accountId: number, request: ImportProfileRequest): Observable<ImportProfile> {
    return this.http.put<ImportProfile>(`${this.baseUrl}/api/v1/accounts/${accountId}/import/profile`, request);
  }

  uploadCsv(accountId: number, file: File): Observable<UploadResponse> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<UploadResponse>(`${this.baseUrl}/api/v1/accounts/${accountId}/import/upload`, formData);
  }

  // Institution endpoints
  createInstitution(request: InstitutionCreateRequest): Observable<Institution> {
    return this.http.post<Institution>(`${this.baseUrl}/api/v1/institutions`, request);
  }

  deleteInstitution(institutionId: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/api/v1/institutions/${institutionId}`);
  }

  getInstitution(institutionId: number): Observable<Institution> {
    return this.http.get<Institution>(`${this.baseUrl}/api/v1/institutions/${institutionId}`);
  }

  getInstitutions(): Observable<Institution[]> {
    return this.http.get<Institution[]>(`${this.baseUrl}/api/v1/institutions`);
  }

  updateInstitution(institutionId: number, request: InstitutionUpdateRequest): Observable<Institution> {
    return this.http.put<Institution>(`${this.baseUrl}/api/v1/institutions/${institutionId}`, request);
  }
}
