import { TestBed, ComponentFixture } from '@angular/core/testing';
import { FormBuilder } from '@angular/forms';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { App, dateRangeValidator, passwordMatchValidator, passwordStrengthValidator } from './app';

describe('App Component and Domain Logic', () => {
  let fixture: ComponentFixture<App>;
  let component: App;
  let httpMock: HttpTestingController;

  let store: Record<string, string> = {};
  const mockLocalStorage = {
    getItem: (key: string) => store[key] || null,
    setItem: (key: string, value: string) => { store[key] = value; },
    removeItem: (key: string) => { delete store[key]; },
    clear: () => { store = {}; }
  };

  beforeEach(() => {
    store = {};
    spyOn(localStorage, 'getItem').and.callFake(mockLocalStorage.getItem);
    spyOn(localStorage, 'setItem').and.callFake(mockLocalStorage.setItem);
    spyOn(localStorage, 'removeItem').and.callFake(mockLocalStorage.removeItem);
    spyOn(localStorage, 'clear').and.callFake(mockLocalStorage.clear);

    TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  function createComponent(): void {
    fixture = TestBed.createComponent(App);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  describe('Pure Logic Validators', () => {
    let fb: FormBuilder;

    beforeEach(() => {
      fb = new FormBuilder();
    });

    it('should pass for valid date ranges (startDate <= endDate)', () => {
      const group = fb.group({
        startDate: ['2023-01-01'],
        endDate: ['2023-12-31'],
        isCurrent: [false]
      }, { validators: [dateRangeValidator] });

      expect(group.valid).toBeTrue();
      expect(group.errors).toBeNull();
    });

    it('should return dateRangeInvalid error when endDate < startDate', () => {
      const group = fb.group({
        startDate: ['2023-12-31'],
        endDate: ['2023-01-01'],
        isCurrent: [false]
      }, { validators: [dateRangeValidator] });

      expect(group.errors).toEqual({ dateRangeInvalid: true });
    });

    it('should be valid when isCurrent is true even if endDate is earlier or present', () => {
      const group = fb.group({
        startDate: ['2023-12-31'],
        endDate: ['2023-01-01'],
        isCurrent: [true]
      }, { validators: [dateRangeValidator] });

      expect(group.errors).toBeNull();
    });

    it('should return null when password and confirmPassword match', () => {
      const registerForm = fb.group({
        password: ['Secret123!'],
        confirmPassword: ['Secret123!']
      }, { validators: [passwordMatchValidator] });

      expect(registerForm.errors).toBeNull();
    });

    it('should return passwordMismatch error when passwords do not match', () => {
      const registerForm = fb.group({
        password: ['Secret123!'],
        confirmPassword: ['Different123!']
      }, { validators: [passwordMatchValidator] });

      expect(registerForm.errors).toEqual({ passwordMismatch: true });
    });

    it('should accept a strong password meeting the policy', () => {
      const control = fb.control('Secret123!');
      expect(passwordStrengthValidator(control)).toBeNull();
    });

    it('should reject a password that is too short', () => {
      const control = fb.control('Sec1!');
      expect(passwordStrengthValidator(control)).toEqual({ passwordStrength: true });
    });

    it('should reject a password missing an uppercase letter', () => {
      const control = fb.control('secret123!');
      expect(passwordStrengthValidator(control)).toEqual({ passwordStrength: true });
    });

    it('should reject a password missing a symbol', () => {
      const control = fb.control('Secret1234');
      expect(passwordStrengthValidator(control)).toEqual({ passwordStrength: true });
    });
  });

  describe('Component Initialization and Auth Forms', () => {
    it('should create the App component instance', () => {
      createComponent();
      expect(component).toBeTruthy();
    });

    it('should initialize without a session and expose login/register forms', () => {
      createComponent();
      expect(component.currentUser).toBeNull();
      expect(component.loginForm.valid).toBeFalse();

      component.loginForm.patchValue({ email: 'user@careeros.com', password: 'password123' });
      expect(component.loginForm.valid).toBeTrue();
    });

    it('should validate registerForm password match', () => {
      createComponent();
      component.registerForm.patchValue({
        fullName: 'User Test',
        email: 'user@careeros.com',
        password: 'Password1!',
        confirmPassword: 'Password2!'
      });
      expect(component.registerForm.hasError('passwordMismatch')).toBeTrue();

      component.registerForm.patchValue({ confirmPassword: 'Password1!' });
      expect(component.registerForm.hasError('passwordMismatch')).toBeFalse();
    });

    it('should validate registerForm required fields', () => {
      createComponent();
      expect(component.registerForm.valid).toBeFalse();

      component.registerForm.patchValue({
        fullName: 'User Test',
        professionalTitle: 'Dev',
        email: 'user@careeros.com',
        password: 'Password1!',
        confirmPassword: 'Password1!'
      });
      expect(component.registerForm.valid).toBeTrue();
    });
  });

  describe('Profile Form Structure and Dynamic Lists', () => {
    beforeEach(() => {
      createComponent();
    });

    it('should start at step 1 with candidateId null', () => {
      expect(component.currentStep).toBe(1);
      expect(component.candidateId).toBeNull();
    });

    it('should validate profile form required fields', () => {
      expect(component.profileForm.invalid).toBeTrue();
      component.profileForm.patchValue({
        fullName: 'John Doe',
        professionalTitle: 'Software Developer',
        email: 'john@example.com'
      });
      expect(component.profileForm.valid).toBeTrue();
    });

    it('should reject invalid email format in profile form', () => {
      component.profileForm.get('email')?.setValue('invalid-email');
      expect(component.profileForm.get('email')?.hasError('email')).toBeTrue();

      component.profileForm.get('email')?.setValue('valid@example.com');
      expect(component.profileForm.get('email')?.hasError('email')).toBeFalse();
    });

    it('should add and remove experience entries', () => {
      component.addExperience({ companyName: 'Tech Corp', jobTitle: 'Engineer' });
      component.addExperience({ companyName: 'Acme', jobTitle: 'Manager' });
      expect(component.experiencesArray.length).toBe(2);

      component.removeExperience(0);
      expect(component.experiencesArray.length).toBe(1);
      expect(component.experiencesArray.at(0).value.companyName).toBe('Acme');
    });

    it('should add and remove education entries', () => {
      component.addEducation({ institution: 'USP', degree: 'BS', fieldOfStudy: 'CS' });
      expect(component.educationsArray.length).toBe(1);

      component.removeEducation(0);
      expect(component.educationsArray.length).toBe(0);
    });

    it('should add and remove certification entries', () => {
      component.addCertification({ name: 'AWS Certified' });
      expect(component.certificationsArray.length).toBe(1);

      component.removeCertification(0);
      expect(component.certificationsArray.length).toBe(0);
    });

    it('should swap experience entries', () => {
      component.addExperience({ companyName: 'Alpha' });
      component.addExperience({ companyName: 'Beta' });

      component.swapExperiences(0, 1);
      expect(component.experiencesArray.at(0).value.companyName).toBe('Beta');
      expect(component.experiencesArray.at(1).value.companyName).toBe('Alpha');
    });

    it('should navigate stepper within bounds', () => {
      component.nextStep();
      expect(component.currentStep).toBe(2);
      component.prevStep();
      expect(component.currentStep).toBe(1);
    });
  });

  describe('saveProfileToApi and Canonical Payload Mapping', () => {
    beforeEach(() => {
      createComponent();
    });

    it('should send POST with canonical schema payload when no candidate id exists', () => {
      const apiUrl = component.apiUrl;
      component.profileForm.patchValue({
        fullName: 'Alice Smith',
        professionalTitle: 'Engineer',
        email: 'alice@example.com'
      });
      component.addExperience({
        companyName: 'Acme Inc',
        jobTitle: 'Developer',
        description: 'Building apps',
        startDate: '2021-01-01',
        isCurrent: true
      });
      component.addEducation({ institution: 'USP', degree: 'BS', fieldOfStudy: 'Computer Science', startDate: '2015-01-01', endDate: '2018-12-01' });
      component.addCertification({ name: 'AWS Certified', issuingOrganization: 'Amazon' });

      component.saveProfileToApi();

      const req = httpMock.expectOne(`${apiUrl}/candidate-profiles`);
      expect(req.request.method).toBe('POST');
      const payload = req.request.body;

      expect(payload.fullName).toBe('Alice Smith');
      expect(payload.workExperiences).toEqual([
        jasmine.objectContaining({
          company: 'Acme Inc',
          role: 'Developer',
          isCurrent: true
        })
      ]);
      expect(payload.educationHistory).toEqual([
        jasmine.objectContaining({ institution: 'USP', course: 'Computer Science' })
      ]);
      expect(payload.certifications).toEqual([
        jasmine.objectContaining({ name: 'AWS Certified', issuer: 'Amazon' })
      ]);

      req.flush({ id: 'new-profile-99' }, { status: 201, statusText: 'Created' });

      expect(component.candidateId).toBe('new-profile-99');
      httpMock.expectOne(`${apiUrl}/resumes/by-candidate/new-profile-99`);
    });

    it('should send PUT request when a candidate id exists', () => {
      const apiUrl = component.apiUrl;
      component.candidateId = 'existing-123';
      component.profileForm.patchValue({
        fullName: 'Bob Johnson',
        professionalTitle: 'Manager',
        email: 'bob@example.com'
      });

      component.saveProfileToApi();

      const req = httpMock.expectOne(`${apiUrl}/candidate-profiles/existing-123`);
      expect(req.request.method).toBe('PUT');
      req.flush({ id: 'existing-123' }, { status: 200, statusText: 'OK' });

      httpMock.expectOne(`${apiUrl}/resumes/by-candidate/existing-123`).flush([]);
    });

    it('should report failure when the API rejects the request', () => {
      const apiUrl = component.apiUrl;
      component.profileForm.patchValue({
        fullName: 'Bob Johnson',
        professionalTitle: 'Manager',
        email: 'bob@example.com'
      });

      component.saveProfileToApi();

      const req = httpMock.expectOne(`${apiUrl}/candidate-profiles`);
      req.flush({ message: 'error' }, { status: 500, statusText: 'Server Error' });

      expect(component.isSuccess).toBeFalse();
      expect(component.statusMessage).toContain('Falha ao salvar o perfil');
    });
  });

  describe('Logout', () => {
    it('should clear session and reset state on logout()', () => {
      createComponent();
      component.logout();

      expect(localStorage.getItem('careeros_user_session')).toBeNull();
      expect(localStorage.getItem('careeros_profile_draft')).toBeNull();
      expect(component.currentUser).toBeNull();
      expect(component.candidateId).toBeNull();
      expect(component.currentStep).toBe(1);
    });
  });
});