import { Injectable, signal } from '@angular/core';
import { UserSession } from './auth.models';

@Injectable({
  providedIn: 'root'
})
export class AuthSessionService {
  private readonly storageKey = 'careeros_user_session';
  readonly currentSession = signal<UserSession | null>(this.getValidSessionFromStorage());

  setSession(session: UserSession): void {
    if (session && session.accessToken && !this.isExpired(session.expiresAt)) {
      localStorage.setItem(this.storageKey, JSON.stringify(session));
      this.currentSession.set(session);
      window.dispatchEvent(new CustomEvent('careeros-auth-changed'));
    }
  }

  getSession(): UserSession | null {
    const session = this.currentSession();
    if (session && this.isExpired(session.expiresAt)) {
      this.clearSession();
      return null;
    }
    return session;
  }

  clearSession(): void {
    localStorage.removeItem(this.storageKey);
    localStorage.removeItem('careeros_profile_draft');
    this.currentSession.set(null);
  }

  isLoggedIn(): boolean {
    return this.getSession() !== null;
  }

  getToken(): string | null {
    const session = this.getSession();
    return session ? session.accessToken : null;
  }

  private getValidSessionFromStorage(): UserSession | null {
    const raw = localStorage.getItem(this.storageKey);
    if (!raw) return null;

    try {
      const session = JSON.parse(raw) as UserSession;
      if (session && session.accessToken && !this.isExpired(session.expiresAt)) {
        return session;
      }
    } catch {
      // Invalid JSON
    }

    localStorage.removeItem(this.storageKey);
    return null;
  }

  private isExpired(expiresAt: string): boolean {
    if (!expiresAt) return true;
    const expTime = new Date(expiresAt).getTime();
    return isNaN(expTime) || expTime <= Date.now();
  }
}
