import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { AuthSessionService } from './auth-session.service';
import { UserSession } from './auth.models';

@Component({
  selector: 'app-google-callback',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="callback-container" style="display: flex; flex-direction: column; align-items: center; justify-content: center; min-height: 300px; font-family: sans-serif;">
      @if (loading) {
        <h2>Autenticando com o Google...</h2>
        <p>Aguarde enquanto finalizamos a sua sessão.</p>
      } @else if (error) {
        <h2 style="color: #d9534f;">Falha na Autenticação</h2>
        <p>{{ error }}</p>
        <button (click)="goToLogin()" style="margin-top: 1rem; padding: 0.5rem 1rem; cursor: pointer;">Voltar para o Login</button>
      }
    </div>
  `
})
export class GoogleCallbackComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly http = inject(HttpClient);
  private readonly authSession = inject(AuthSessionService);

  readonly apiUrl = 'https://localhost:7276/api';

  loading = true;
  error = '';

  ngOnInit(): void {
    const code = this.route.snapshot.queryParamMap.get('code');
    if (!code) {
      this.error = 'Código de autorização não encontrado.';
      this.loading = false;
      return;
    }

    this.http.post<UserSession>(`${this.apiUrl}/auth/exchange-google`, { code }).subscribe({
      next: (session) => {
        this.authSession.setSession(session);
        this.loading = false;
        this.router.navigate(['/'], { replaceUrl: true });
      },
      error: (err) => {
        console.error('Exchange Google code failed', err);
        this.loading = false;
        this.error = err.error?.message || 'Código de troca inválido ou expirado. Tente novamente.';
      }
    });
  }

  goToLogin(): void {
    this.router.navigate(['/']);
  }
}
