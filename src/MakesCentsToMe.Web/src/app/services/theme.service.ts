import { Injectable, signal } from '@angular/core';

export type ThemeMode = 'dark' | 'light';

const THEME_STORAGE_KEY = 'theme';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  readonly currentTheme = signal<ThemeMode>(this.resolveInitialTheme());

  applyTheme(theme: ThemeMode): void {
    document.body.classList.remove('dark-theme', 'light-theme');
    document.body.classList.add(`${theme}-theme`);
    localStorage.setItem(THEME_STORAGE_KEY, theme);
    this.currentTheme.set(theme);
  }

  initialize(): void {
    this.applyTheme(this.resolveInitialTheme());
  }

  toggleTheme(): void {
    const next: ThemeMode = this.currentTheme() === 'dark' ? 'light' : 'dark';
    this.applyTheme(next);
  }

  private resolveInitialTheme(): ThemeMode {
    const stored = localStorage.getItem(THEME_STORAGE_KEY) as ThemeMode | null;
    if (stored === 'dark' || stored === 'light') {
      return stored;
    }
    const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
    return prefersDark ? 'dark' : 'light';
  }
}
