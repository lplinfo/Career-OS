import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import { GoogleCallbackComponent } from './google-callback.component';
import { AuthSessionService } from './auth-session.service';

describe('GoogleCallbackComponent', () => {
  let component: GoogleCallbackComponent;
  let fixture: ComponentFixture<GoogleCallbackComponent>;
  let httpMock: HttpTestingController;
  let authService: AuthSessionService;
  let routerSpy: jasmine.SpyObj<Router>;
  let mockActivatedRoute: any;

  beforeEach(async () => {
    routerSpy = jasmine.createSpyObj('Router', ['navigate']);
    mockActivatedRoute = {
      snapshot: {
        queryParamMap: {
          get: (key: string) => (key === 'code' ? 'valid_code_123' : null)
        }
      }
    };
    localStorage.clear();

    await TestBed.configureTestingModule({
      imports: [GoogleCallbackComponent],
      providers: [
        AuthSessionService,
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: Router, useValue: routerSpy },
        { provide: ActivatedRoute, useValue: mockActivatedRoute }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(GoogleCallbackComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
    authService = TestBed.inject(AuthSessionService);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('should exchange code and establish session on init', () => {
    fixture.detectChanges(); // triggers ngOnInit

    const req = httpMock.expectOne('https://localhost:7276/api/auth/exchange-google');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ code: 'valid_code_123' });

    req.flush({
      userId: 'u1',
      email: 'googleuser@example.com',
      candidateProfileId: 'c1',
      fullName: 'Google User',
      accessToken: 'jwt_google_token',
      tokenType: 'Bearer',
      expiresAt: new Date(Date.now() + 3600000).toISOString()
    });

    expect(authService.isLoggedIn()).toBeTrue();
    expect(authService.getToken()).toBe('jwt_google_token');
  });

  it('should display error when code is missing', () => {
    mockActivatedRoute.snapshot.queryParamMap.get = () => null;

    fixture.detectChanges(); // triggers ngOnInit

    expect(component.loading).toBeFalse();
    expect(component.error).toBe('Código de autorização não encontrado.');
  });
});
