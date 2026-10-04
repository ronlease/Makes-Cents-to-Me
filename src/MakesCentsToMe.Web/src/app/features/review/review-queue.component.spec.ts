import { TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { of } from 'rxjs';
import { vi } from 'vitest';
import {
  ApiService,
  LearnedRuleSuggestion,
  ReviewTransaction,
} from '../../services/api.service';
import { ReviewQueueComponent } from './review-queue.component';

const suggestion: LearnedRuleSuggestion = {
  categoryId: 'c1',
  categoryName: 'Groceries',
  existingLearnedRuleId: null,
  normalizedVendor: 'Whole Foods',
  pattern: 'WHOLEFDS MKT',
  sourceTransactionId: 't1',
};

function buildTransaction(learnedRuleSuggestion: LearnedRuleSuggestion | null): ReviewTransaction {
  return { id: 't1', learnedRuleSuggestion, status: 'Committed' } as ReviewTransaction;
}

describe('ReviewQueueComponent learned rule suggestion', () => {
  const overrideResult = { categoryId: 'c1', normalizedVendor: 'Whole Foods' };
  const ruleResult = { categoryId: 'c1', normalizedVendor: 'Whole Foods', pattern: 'WHOLEFDS MKT' };
  let apiService: Record<string, ReturnType<typeof vi.fn>>;
  let dialogOpen: ReturnType<typeof vi.fn>;

  function setup(updated: ReviewTransaction) {
    apiService = {
      createLearnedRule: vi.fn(() => of({})),
      getCategories: vi.fn(() => of([])),
      getReviewQueue: vi.fn(() => of([])),
      overrideTransaction: vi.fn(() => of(updated)),
      updateLearnedRule: vi.fn(() => of({})),
    };
    dialogOpen = vi
      .fn()
      .mockReturnValueOnce({ afterClosed: () => of(overrideResult) })
      .mockReturnValue({ afterClosed: () => of(ruleResult) });
    TestBed.configureTestingModule({
      imports: [ReviewQueueComponent],
      providers: [{ provide: ApiService, useValue: apiService }],
    });
    TestBed.overrideProvider(MatDialog, { useValue: { open: dialogOpen } });
    const fixture = TestBed.createComponent(ReviewQueueComponent);
    fixture.componentInstance.openOverrideDialog(buildTransaction(null));
  }

  it('does not open the suggestion dialog without a suggestion', () => {
    setup(buildTransaction(null));
    expect(dialogOpen).toHaveBeenCalledTimes(1);
  });

  it('creates a rule when there is no existing rule', () => {
    setup(buildTransaction(suggestion));
    expect(dialogOpen).toHaveBeenCalledTimes(2);
    expect(apiService['createLearnedRule']).toHaveBeenCalledWith({
      ...ruleResult,
      sourceTransactionId: 't1',
    });
    expect(apiService['updateLearnedRule']).not.toHaveBeenCalled();
  });

  it('updates the existing rule when one exists', () => {
    setup(buildTransaction({ ...suggestion, existingLearnedRuleId: 'r9' }));
    expect(apiService['updateLearnedRule']).toHaveBeenCalledWith('r9', ruleResult);
    expect(apiService['createLearnedRule']).not.toHaveBeenCalled();
  });
});
