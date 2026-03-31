import { Component, inject, OnInit, signal, ViewChild } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatRadioModule } from '@angular/material/radio';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatStepper, MatStepperModule } from '@angular/material/stepper';
import { MatTableModule } from '@angular/material/table';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ApiService, ImportProfile, ImportResult, UploadPreviewResponse } from '../../services/api.service';

type ApplicationField =
  | 'Amount'
  | 'Balance'
  | 'Category'
  | 'CheckNumber'
  | 'Date'
  | 'Description'
  | 'Fees'
  | 'Ignore'
  | 'Interest'
  | 'Principal';

const APPLICATION_FIELDS: { label: string; value: ApplicationField }[] = [
  { label: 'Amount', value: 'Amount' },
  { label: 'Balance', value: 'Balance' },
  { label: 'Category', value: 'Category' },
  { label: 'Check Number', value: 'CheckNumber' },
  { label: 'Date', value: 'Date' },
  { label: 'Description', value: 'Description' },
  { label: 'Fees', value: 'Fees' },
  { label: 'Ignore', value: 'Ignore' },
  { label: 'Interest', value: 'Interest' },
  { label: 'Principal', value: 'Principal' },
];

const DATE_FORMAT_PRESETS = [
  'M/d/yyyy',
  'M/d/yyyy H:mm',
  'MM/dd/yyyy',
  'yyyy-MM-dd',
  'MM-dd-yyyy',
];

@Component({
  selector: 'app-import',
  standalone: true,
  imports: [
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatRadioModule,
    MatSelectModule,
    MatSlideToggleModule,
    MatStepperModule,
    MatTableModule,
    MatToolbarModule,
    MatTooltipModule,
    ReactiveFormsModule,
    RouterModule,
  ],
  template: `
    <mat-toolbar>
      <button mat-icon-button matTooltip="Back to Accounts" [routerLink]="backRoute()">
        <mat-icon>arrow_back</mat-icon>
      </button>
      <span>Import Transactions</span>
    </mat-toolbar>

    <div class="import-container">
      <mat-stepper linear #stepper>

        <!-- Step 1: Upload -->
        <mat-step label="Upload CSV" [completed]="csvPreview() !== null">
          <div class="step-content">
            @if (!csvPreview()) {
              <div class="upload-zone" tabindex="0" role="button" (click)="fileInput.click()" (keydown.enter)="fileInput.click()" (dragover)="onDragOver($event)" (drop)="onFileDrop($event)">
                <mat-icon class="upload-icon">cloud_upload</mat-icon>
                <p>Click or drag and drop a CSV file here</p>
                <input #fileInput type="file" accept=".csv" hidden (change)="onFileSelected($event)" />
              </div>
              @if (isUploading()) {
                <div class="loading-container">
                  <mat-spinner diameter="40"></mat-spinner>
                  <span>Uploading...</span>
                </div>
              }
            } @else {
              <div class="preview-section">
                <h3>Detected Headers</h3>
                <div class="chip-row">
                  @for (header of csvPreview()!.headers; track header) {
                    <span class="header-chip">{{ header }}</span>
                  }
                </div>

                <h3>Preview (first rows)</h3>
                <div class="table-container">
                  <table mat-table [dataSource]="csvPreview()!.previewRows" class="preview-table">
                    @for (header of csvPreview()!.headers; track header; let colIndex = $index) {
                      <ng-container [matColumnDef]="header">
                        <th mat-header-cell *matHeaderCellDef>{{ header }}</th>
                        <td mat-cell *matCellDef="let row">{{ row[colIndex] }}</td>
                      </ng-container>
                    }
                    <tr mat-header-row *matHeaderRowDef="csvPreview()!.headers"></tr>
                    <tr mat-row *matRowDef="let row; columns: csvPreview()!.headers;"></tr>
                  </table>
                </div>

                <div class="step-actions">
                  <button mat-button (click)="resetUpload()">Re-upload</button>
                  <button mat-flat-button matStepperNext (click)="advanceFromUpload()">Next</button>
                </div>
              </div>
            }
          </div>
        </mat-step>

        <!-- Step 2: Map Columns -->
        <mat-step label="Map Columns" [completed]="columnMappingComplete()">
          <div class="step-content">
            @if (existingProfile()) {
              <div class="profile-notice">
                <mat-icon>info</mat-icon>
                <span>A saved import profile was found and will be used automatically.</span>
              </div>
              <div class="step-actions">
                <button mat-button (click)="clearSavedProfile()">Change Mapping</button>
                <button mat-flat-button matStepperNext>Next</button>
              </div>
            } @else {
              <form [formGroup]="mappingForm">
                <h3>Column Mappings</h3>
                <div class="mapping-grid">
                  @for (header of detectedHeaders(); track header) {
                    <mat-form-field appearance="outline">
                      <mat-label>{{ header }}</mat-label>
                      <mat-select [formControlName]="header">
                        @for (field of applicationFields; track field.value) {
                          <mat-option [value]="field.value">{{ field.label }}</mat-option>
                        }
                      </mat-select>
                    </mat-form-field>
                  }
                </div>

                <h3>Date Format</h3>
                <mat-form-field appearance="outline" class="date-format-field">
                  <mat-label>Date Format</mat-label>
                  <input matInput [formControl]="dateFormatControl" placeholder="M/d/yyyy" />
                  <mat-hint>Common: {{ dateFormatPresets.join(' · ') }}</mat-hint>
                </mat-form-field>

                <div class="toggle-row">
                  <mat-slide-toggle [formControl]="balanceProvidedControl">
                    Balance provided in CSV
                  </mat-slide-toggle>
                </div>

                <h3>Amount Type</h3>
                <mat-radio-group [formControl]="amountTypeControl" class="radio-group">
                  <mat-radio-button value="Single">Single column (positive = credit, negative = debit)</mat-radio-button>
                  <mat-radio-button value="Split">Split columns (separate debit / credit columns)</mat-radio-button>
                </mat-radio-group>

                <div class="step-actions">
                  <button mat-button matStepperPrevious>Back</button>
                  <button mat-flat-button (click)="saveProfile()">Save &amp; Continue</button>
                </div>
              </form>
            }
          </div>
        </mat-step>

        <!-- Step 3: Process -->
        <mat-step label="Process">
          <div class="step-content">
            @if (!(existingProfile()?.balanceProvided || savedProfile()?.balanceProvided)) {
              <h3>Opening / Closing Balance</h3>
              <div class="balance-form">
                <mat-radio-group [formControl]="balanceTypeControl" class="radio-group">
                  <mat-radio-button value="opening">Opening Balance</mat-radio-button>
                  <mat-radio-button value="closing">Closing Balance</mat-radio-button>
                </mat-radio-group>

                <mat-form-field appearance="outline">
                  <mat-label>{{ balanceTypeControl.value === 'opening' ? 'Opening Balance' : 'Closing Balance' }}</mat-label>
                  <span matTextPrefix>$&nbsp;</span>
                  <input matInput type="number" [formControl]="balanceAmountControl" placeholder="0.00" />
                </mat-form-field>
              </div>
            }

            @if (isProcessing()) {
              <div class="loading-container">
                <mat-spinner diameter="40"></mat-spinner>
                <span>Processing...</span>
              </div>
            } @else if (importResult()) {
              <div class="result-section">
                <mat-icon class="success-icon">check_circle</mat-icon>
                <h2>Import Complete</h2>
                <p>Transactions created: <strong>{{ importResult()!.transactionsCreated }}</strong></p>
                <p>Duplicates skipped: <strong>{{ importResult()!.duplicatesSkipped }}</strong></p>
                <p>Rows skipped: <strong>{{ importResult()!.rowsSkipped }}</strong></p>
                <div class="step-actions">
                  <button mat-button [routerLink]="backRoute()">Back to Institutions</button>
                  <button mat-flat-button routerLink="/review">Review Transactions</button>
                </div>
              </div>
            } @else {
              <div class="step-actions">
                <button mat-button matStepperPrevious>Back</button>
                <button mat-flat-button (click)="processImport()">Process Import</button>
              </div>
            }
          </div>
        </mat-step>

      </mat-stepper>
    </div>
  `,
  styles: [`
    .import-container {
      max-width: 900px;
      margin: 24px auto;
      padding: 0 16px;
    }
    .step-content {
      padding: 24px 0 16px;
    }
    .upload-zone {
      border: 2px dashed var(--mat-sys-outline);
      border-radius: 8px;
      padding: 48px;
      text-align: center;
      cursor: pointer;
      transition: background-color 0.2s;
    }
    .upload-zone:hover {
      background-color: var(--mat-sys-surface-variant);
    }
    .upload-icon {
      font-size: 48px;
      width: 48px;
      height: 48px;
      color: var(--mat-sys-primary);
    }
    .loading-container {
      display: flex;
      align-items: center;
      gap: 16px;
      padding: 16px 0;
    }
    .chip-row {
      display: flex;
      flex-wrap: wrap;
      gap: 8px;
      margin-bottom: 16px;
    }
    .header-chip {
      background: var(--mat-sys-secondary-container);
      color: var(--mat-sys-on-secondary-container);
      border-radius: 16px;
      padding: 4px 12px;
      font-size: 13px;
    }
    .table-container {
      overflow-x: auto;
      margin-bottom: 16px;
    }
    .preview-table {
      min-width: 100%;
    }
    .step-actions {
      display: flex;
      gap: 8px;
      margin-top: 16px;
    }
    .mapping-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
      gap: 8px;
      margin-bottom: 16px;
    }
    .date-format-field {
      width: 280px;
      margin-bottom: 16px;
    }
    .toggle-row {
      margin: 16px 0;
    }
    .radio-group {
      display: flex;
      flex-direction: column;
      gap: 8px;
      margin-bottom: 16px;
    }
    .balance-form {
      display: flex;
      flex-direction: column;
      gap: 16px;
      max-width: 320px;
      margin-bottom: 24px;
    }
    .profile-notice {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 16px;
      background: var(--mat-sys-surface-variant);
      border-radius: 8px;
      margin-bottom: 16px;
    }
    .result-section {
      text-align: center;
      padding: 32px;
    }
    .success-icon {
      font-size: 64px;
      width: 64px;
      height: 64px;
      color: green;
    }
  `],
})
export class ImportComponent implements OnInit {
  @ViewChild('stepper') private stepper!: MatStepper;

  protected readonly accountId = signal<string>('');
  protected readonly applicationFields = APPLICATION_FIELDS;
  protected readonly amountTypeControl = new FormControl<'Single' | 'Split'>('Single', { nonNullable: true });
  protected readonly balanceAmountControl = new FormControl<number | null>(null);
  protected readonly balanceProvidedControl = new FormControl<boolean>(false, { nonNullable: true });
  protected readonly balanceTypeControl = new FormControl<'closing' | 'opening'>('opening', { nonNullable: true });
  protected readonly columnMappingComplete = signal(false);
  protected readonly csvPreview = signal<UploadPreviewResponse | null>(null);
  protected readonly dateFormatControl = new FormControl<string>('M/d/yyyy', { nonNullable: true, validators: [Validators.required] });
  protected readonly dateFormatPresets = DATE_FORMAT_PRESETS;
  protected readonly detectedHeaders = signal<string[]>([]);
  protected readonly existingProfile = signal<ImportProfile | null>(null);
  protected readonly importResult = signal<ImportResult | null>(null);
  protected readonly isProcessing = signal(false);
  protected readonly isUploading = signal(false);
  protected readonly uploadedFile = signal<File | null>(null);
  protected readonly mappingForm: FormGroup = new FormGroup({});
  protected readonly savedProfile = signal<ImportProfile | null>(null);

  private readonly apiService = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly snackBar = inject(MatSnackBar);

  get backRoute(): () => string[] {
    return () => ['/institutions'];
  }

  advanceFromUpload(): void {
    const headers = this.csvPreview()?.headers ?? [];
    this.detectedHeaders.set(headers);
    this.buildMappingForm(headers);
    this.loadExistingProfile();
  }

  clearSavedProfile(): void {
    const profile = this.existingProfile() ?? this.savedProfile();
    this.existingProfile.set(null);
    this.columnMappingComplete.set(false);

    if (profile) {
      this.amountTypeControl.setValue(profile.amountType);
      this.balanceProvidedControl.setValue(profile.balanceProvided);
      this.dateFormatControl.setValue(profile.dateFormat);

      const headers = this.detectedHeaders();
      this.buildMappingForm(headers);
      for (const mapping of profile.columnMappings) {
        const control = this.mappingForm.controls[mapping.csvColumnName];
        if (control) {
          control.setValue(mapping.applicationField);
        }
      }
    }
  }

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('accountId');
    this.accountId.set(idParam ?? '');
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
  }

  onFileDrop(event: DragEvent): void {
    event.preventDefault();
    const file = event.dataTransfer?.files?.[0];
    if (file) this.uploadFile(file);
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (file) this.uploadFile(file);
  }

  processImport(): void {
    const file = this.uploadedFile();
    if (!file) {
      this.snackBar.open('No file uploaded. Please go back and upload a CSV.', 'Dismiss', { duration: 4000 });
      return;
    }

    this.isProcessing.set(true);
    const profile = this.savedProfile() ?? this.existingProfile();
    const balanceProvided = profile?.balanceProvided ?? false;
    const request = balanceProvided
      ? {}
      : this.balanceTypeControl.value === 'opening'
        ? { openingBalance: this.balanceAmountControl.value ?? undefined }
        : { closingBalance: this.balanceAmountControl.value ?? undefined };

    this.apiService.processImport(this.accountId(), file, request).subscribe({
      next: result => {
        this.importResult.set(result);
        this.isProcessing.set(false);
      },
      error: () => {
        this.snackBar.open('Failed to process import.', 'Dismiss', { duration: 4000 });
        this.isProcessing.set(false);
      },
    });
  }

  resetUpload(): void {
    this.csvPreview.set(null);
    this.detectedHeaders.set([]);
  }

  saveProfile(): void {
    const columnMappings = this.detectedHeaders().map(header => ({
      applicationField: this.mappingForm.controls[header]?.value ?? 'Ignore',
      csvColumnName: header,
    }));
    const request = {
      amountType: this.amountTypeControl.value,
      balanceProvided: this.balanceProvidedControl.value,
      columnMappings,
      dateFormat: this.dateFormatControl.value,
    };
    const profileExists = this.existingProfile() ?? this.savedProfile();
    const save$ = profileExists
      ? this.apiService.updateImportProfile(this.accountId(), request)
      : this.apiService.saveImportProfile(this.accountId(), request);

    save$.subscribe({
      next: profile => {
        this.savedProfile.set(profile);
        this.columnMappingComplete.set(true);
        setTimeout(() => this.stepper.next());
        this.snackBar.open('Import profile saved.', 'Dismiss', { duration: 3000 });
      },
      error: () => {
        this.snackBar.open('Failed to save import profile.', 'Dismiss', { duration: 4000 });
      },
    });
  }

  private buildMappingForm(headers: string[]): void {
    const controls: Record<string, FormControl<string>> = {};
    for (const header of headers) {
      const guessed = this.guessApplicationField(header);
      controls[header] = new FormControl<string>(guessed, { nonNullable: true });
    }
    Object.keys(this.mappingForm.controls).forEach(key => this.mappingForm.removeControl(key));
    Object.entries(controls).forEach(([key, control]) => this.mappingForm.addControl(key, control));
  }

  private guessApplicationField(header: string): string {
    const normalized = header.toLowerCase().replace(/[^a-z]/g, '');
    const guessMap: Record<string, string> = {
      amount: 'Amount',
      balance: 'Balance',
      category: 'Category',
      check: 'CheckNumber',
      checknumber: 'CheckNumber',
      checkno: 'CheckNumber',
      date: 'Date',
      description: 'Description',
      fees: 'Fees',
      fee: 'Fees',
      interest: 'Interest',
      memo: 'Description',
      principal: 'Principal',
      transactiondate: 'Date',
      transdate: 'Date',
      postdate: 'Date',
      postingdate: 'Date',
    };
    return guessMap[normalized] ?? 'Ignore';
  }

  private loadExistingProfile(): void {
    this.apiService.getImportProfile(this.accountId()).subscribe({
      next: profile => {
        this.existingProfile.set(profile);
        this.columnMappingComplete.set(true);
        this.savedProfile.set(profile);
      },
      error: () => {
        // No existing profile — user will map columns
        this.existingProfile.set(null);
      },
    });
  }

  private uploadFile(file: File): void {
    this.isUploading.set(true);
    this.uploadedFile.set(file);
    this.apiService.uploadCsv(this.accountId(), file).subscribe({
      next: response => {
        this.csvPreview.set(response);
        this.isUploading.set(false);
      },
      error: () => {
        this.snackBar.open('Failed to upload file.', 'Dismiss', { duration: 4000 });
        this.isUploading.set(false);
      },
    });
  }
}
