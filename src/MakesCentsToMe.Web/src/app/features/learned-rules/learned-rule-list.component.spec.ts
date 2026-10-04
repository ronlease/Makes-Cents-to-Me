import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { ApiService, LearnedRule } from '../../services/api.service';
import { LearnedRuleListComponent } from './learned-rule-list.component';

const rule: LearnedRule = {
  categoryId: 'c1',
  categoryName: 'Groceries',
  createdAt: '2026-01-01T00:00:00Z',
  id: 'r1',
  normalizedVendor: 'Whole Foods',
  pattern: 'WHOLEFDS MKT',
  updatedAt: '2026-01-02T00:00:00Z',
};

describe('LearnedRuleListComponent', () => {
  const apiService = {
    deleteLearnedRule: vi.fn(() => of(undefined)),
    getCategories: vi.fn(() => of([])),
    getLearnedRules: vi.fn(() => of([rule])),
  };

  beforeEach(() => {
    vi.clearAllMocks();
    TestBed.configureTestingModule({
      imports: [LearnedRuleListComponent],
      providers: [{ provide: ApiService, useValue: apiService }],
    });
  });

  it('renders a row per rule', async () => {
    const fixture = TestBed.createComponent(LearnedRuleListComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('WHOLEFDS MKT');
    expect(fixture.nativeElement.textContent).toContain('Whole Foods');
  });

  it('deletes a rule after confirmation', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    const fixture = TestBed.createComponent(LearnedRuleListComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.componentInstance.deleteRule(rule);
    expect(apiService.deleteLearnedRule).toHaveBeenCalledWith('r1');
  });
});
