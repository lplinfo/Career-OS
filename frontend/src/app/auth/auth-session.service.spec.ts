import { TestBed } from '@angular/core/testing';
import { AuthSessionService } from './auth-session.service';
import { UserSession } from './auth.models';

describe('AuthSessionService', () => {
  let service: AuthSessionService;

  const validSession: UserSession = {
    userId: '11111111-1111-1111-1111-111111111111',
    email: 'test@example.com',
    candidateProfileId: '22222222-2222-2222-2222-222222222222',
    fullName: 'Test User',
    accessToken: 'mock_token',
    tokenType: 'Bearer',
    expiresAt: new Date(Date.now() + 3600000).toISOString()
  };

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({});
    service = TestBed.inject(AuthSessionService);
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should set and retrieve a valid session', () => {
    service.setSession(validSession);
    expect(service.isLoggedIn()).toBeTrue();
    expect(service.getToken()).toBe('mock_token');
    expect(service.getSession()).toEqual(validSession);
  });

  it('should clear session correctly', () => {
    service.setSession(validSession);
    service.clearSession();
    expect(service.isLoggedIn()).toBeFalse();
    expect(service.getToken()).toBeNull();
    expect(service.getSession()).toBeNull();
  });

  it('should handle expired sessions gracefully', () => {
    const expiredSession: UserSession = {
      ...validSession,
      expiresAt: new Date(Date.now() - 3600000).toISOString()
    };
    service.setSession(expiredSession);
    expect(service.isLoggedIn()).toBeFalse();
    expect(service.getToken()).toBeNull();
  });
});
