import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthResponse, UserSession } from './models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly storageKey = 'smart-helpdesk-session';
  private readonly apiUrl = '/api/auth';
  private readonly sessionState = signal<UserSession | null>(this.readSession());

  readonly session = computed(() => this.sessionState());
  readonly isLoggedIn = computed(() => !!this.sessionState());
  readonly role = computed(() => this.sessionState()?.role ?? null);

  async login(payload: { email: string; password: string }): Promise<UserSession> {
    const response = await firstValueFrom(this.http.post<AuthResponse>(`${this.apiUrl}/login`, payload));
    this.setSession(response);
    return response;
  }

  async register(payload: { name: string; email: string; password: string; role: string }): Promise<UserSession> {
    const response = await firstValueFrom(this.http.post<AuthResponse>(`${this.apiUrl}/register`, payload));
    this.setSession(response);
    return response;
  }

  logout(): void {
    localStorage.removeItem(this.storageKey);
    this.sessionState.set(null);
    this.router.navigateByUrl('/login');
  }

  private setSession(session: UserSession): void {
    localStorage.setItem(this.storageKey, JSON.stringify(session));
    this.sessionState.set(session);
  }

  private readSession(): UserSession | null {
    const rawValue = localStorage.getItem(this.storageKey);
    return rawValue ? JSON.parse(rawValue) as UserSession : null;
  }
}
