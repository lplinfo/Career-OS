import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { Router } from '@angular/router';
import { authInterceptor } from './auth.interceptor';
import { AuthSessionService } from './auth-session.service';

describe('authInterceptor', () => {
  let httpMock: HttpTestingController;
  let httpClient: HttpClient;
  let authService: AuthSessionService;
  let routerSpy: jasmine.SpyObj<Router>;

  beforeEach(() => {
    routerSpy = jasmine.createSpyObj('Router', ['navigate']);
    localStorage.clear();

    TestBed.configureTestingModule({
      providers: [
        AuthSessionService,
        { provide: Router, useValue: routerSpy },
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting()
      ]
    });

    httpMock = TestBed.inject(HttpTestingController);
    httpClient = TestBed.inject(HttpClient);
    authService = TestBed.inject(AuthSessionService);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('should attach Authorization header for protected API calls when session is active', () => {
    authService.setSession({
      userId: 'u1',
      email: 'test@example.com',
      candidateProfileId: 'c1',
      fullName: 'Test',
      accessToken: 'test_token',
      tokenType: 'Bearer',
      expiresAt: new Date(Date.now() + 3600000).toISOString()
    });

    httpClient.get('/api/candidate-profiles/c1').subscribe();

    const req = httpMock.expectOne('/api/candidate-profiles/c1');
    expect(req.request.headers.has('Authorization')).toBeTrue();
    expect(req.request.headers.get('Authorization')).toBe('Bearer test_token');
    req.flush({});
  });

  it('should NOT attach Authorization header for login endpoints', () => {
    authService.setSession({
      userId: 'u1',
      email: 'test@example.com',
      candidateProfileId: 'c1',
      fullName: 'Test',
      accessToken: 'test_token',
      tokenType: 'Bearer',
      expiresAt: new Date(Date.now() + 3600000).toISOString()
    });

    httpClient.post('/api/auth/login', {}).subscribe();

    const req = httpMock.expectOne('/api/auth/login');
    expect(req.request.headers.has('Authorization')).toBeFalse();
    req.flush({});
  });

  it('should clear session and navigate on 401 error', () => {
    authService.setSession({
      userId: 'u1',
      email: 'test@example.com',
      candidateProfileId: 'c1',
      fullName: 'Test',
      accessToken: 'test_token',
      tokenType: 'Bearer',
      expiresAt: new Date(Date.now() + 3600000).toISOString()
    });

    httpClient.get('/api/candidate-profiles/c1').subscribe({
      error: (err) => expect(err.status).toBe(401)
    });

    const req = httpMock.expectOne('/api/candidate-profiles/c1');
    req.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });

    expect(authService.isLoggedIn()).toBeFalse();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/']);
  });
});
