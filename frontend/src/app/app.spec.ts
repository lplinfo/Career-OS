import { TestBed, ComponentFixture } from '@angular/core/testing';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { App, dateRangeValidator, passwordMatchValidator } from './app';
import { CandidateProfileService } from './candidate-profile.service';
import { of, throwError } from 'rxjs';

describe('App Component and Domain Logic', () => {
  let fixture: ComponentFixture<App>;
  let component: App;
  let httpMock: HttpTestingController;
  let profileService: CandidateProfileService;

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
      imports: [App, ReactiveFormsModule],
      providers: [
        CandidateProfileService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    httpMock = TestBed.inject(HttpTestingController);
    profileService = TestBed.inject(CandidateProfileService);
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
    describe('dateRangeValidator', () => {
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
    });

    describe('passwordMatchValidator', () => {
      let fb: FormBuilder;

      beforeEach(() => {
        fb = new FormBuilder();
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
    });
  });

  describe('Component Initialization and Form Structure', () => {
    it('should create the App component instance', () => {
      createComponent();
      expect(component).toBeTruthy();
    });

    it('should initialize with step 0 and correct step labels', () => {
      createComponent();
      expect((component as any).step()).toBe(0);
      expect((component as any).steps).toEqual(['Perfil', 'Experiência', 'Formação', 'Competências']);
    });

    it('should validate profile form required fields', () => {
      createComponent();
      const profileGroup = (component as any).form.get('profile');

      expect(profileGroup.valid).toBeFalse();

      profileGroup.patchValue({
        fullName: 'John Doe',
        professionalTitle: 'Software Developer',
        email: 'john@example.com'
      });

      expect(profileGroup.valid).toBeTrue();
    });

    it('should validate email format in profile form', () => {
      createComponent();
      const emailControl = (component as any).form.get('profile.email');
      emailControl.setValue('invalid-email');

      expect(emailControl.hasError('email')).toBeTrue();

      emailControl.setValue('valid@example.com');
      expect(emailControl.hasError('email')).toBeFalse();
    });

    it('should restore form draft from localStorage on ngOnInit if present', () => {
      const draftData = {
        profile: {
          fullName: 'Jane Saved',
          professionalTitle: 'Architect',
          email: 'jane@example.com',
          phone: '123456789',
          city: 'Sao Paulo',
          country: 'Brazil',
          professionalSummary: 'Summary text',
          openToRemoteWork: true,
          openToRelocation: false
        },
        experience: { company: 'Tech Corp', role: 'Lead', startDate: '2020-01', endDate: '', description: '' },
        education: { institution: 'USP', course: 'CS', degree: 'BS', completionDate: '2019' },
        skills: { languages: 'English', technicalSkills: 'Angular, C#', certifications: 'AWS Cloud' }
      };
      localStorage.setItem('careeros-candidate-draft', JSON.stringify(draftData));

      createComponent();

      expect((component as any).form.get('profile.fullName').value).toBe('Jane Saved');
      expect((component as any).form.get('experience.company').value).toBe('Tech Corp');
    });

    it('should auto-save draft to localStorage on form value changes', () => {
      createComponent();
      (component as any).form.get('profile.fullName').setValue('Updated Name');

      const savedDraft = localStorage.getItem('careeros-candidate-draft');
      expect(savedDraft).not.toBeNull();
      expect(JSON.parse(savedDraft!).profile.fullName).toBe('Updated Name');
      expect((component as any).savedAt()).not.toBeNull();
    });
  });

  describe('Step Navigation', () => {
    beforeEach(() => {
      createComponent();
    });

    it('should navigate forward with next() when not on last step', () => {
      expect((component as any).step()).toBe(0);
      component.next();
      expect((component as any).step()).toBe(1);
      component.next();
      expect((component as any).step()).toBe(2);
      component.next();
      expect((component as any).step()).toBe(3);
      component.next();
      expect((component as any).step()).toBe(3); // does not exceed step count
    });

    it('should navigate backward with previous() when step > 0', () => {
      (component as any).step.set(2);
      component.previous();
      expect((component as any).step()).toBe(1);
      component.previous();
      expect((component as any).step()).toBe(0);
      component.previous();
      expect((component as any).step()).toBe(0); // does not go below 0
    });
  });

  describe('saveProfile Behavior and API Integration', () => {
    beforeEach(() => {
      createComponent();
    });

    it('should reset step to 0 and mark form touched if form is invalid on saveProfile()', () => {
      (component as any).step.set(2);
      expect((component as any).form.invalid).toBeTrue();

      component.saveProfile();

      expect((component as any).step()).toBe(0);
      expect((component as any).form.touched).toBeTrue();
    });

    it('should send POST request when profileId is absent and form is valid', () => {
      (component as any).form.get('profile').patchValue({
        fullName: 'Alice Smith',
        professionalTitle: 'Engineer',
        email: 'alice@example.com'
      });
      (component as any).form.get('experience').patchValue({
        company: 'Acme Inc',
        role: 'Developer',
        startDate: '2021-01-01',
        endDate: '',
        description: 'Building apps'
      });
      (component as any).form.get('skills').patchValue({
        certifications: 'Cert A\nCert B'
      });

      spyOn(profileService, 'save').and.returnValue(of({ id: 'new-profile-99' }));

      component.saveProfile();

      expect(profileService.save).toHaveBeenCalledWith(null, jasmine.objectContaining({
        fullName: 'Alice Smith',
        professionalTitle: 'Engineer',
        email: 'alice@example.com',
        workExperiences: [
          jasmine.objectContaining({ company: 'Acme Inc', isCurrent: true })
        ],
        certifications: [
          jasmine.objectContaining({ name: 'Cert A', displayOrder: 0 }),
          jasmine.objectContaining({ name: 'Cert B', displayOrder: 1 })
        ]
      }));
      expect(localStorage.getItem('careeros-profile-id')).toBe('new-profile-99');
      expect((component as any).saving()).toBeFalse();
      expect((component as any).saveMessage()).toBe('Perfil salvo na API.');
    });

    it('should send PUT request with existing profileId and handle API error gracefully', () => {
      localStorage.setItem('careeros-profile-id', 'existing-123');

      (component as any).form.get('profile').patchValue({
        fullName: 'Bob Johnson',
        professionalTitle: 'Manager',
        email: 'bob@example.com'
      });

      spyOn(profileService, 'save').and.returnValue(throwError(() => new Error('API Error')));

      component.saveProfile();

      expect(profileService.save).toHaveBeenCalledWith('existing-123', jasmine.any(Object));
      expect((component as any).saving()).toBeFalse();
      expect((component as any).saveMessage()).toBe('Não foi possível salvar. Confirme se a API está em execução.');
    });
  });

  describe('Auth Forms, Resume Management, and List Operations on App Component', () => {
    beforeEach(() => {
      createComponent();
    });

    it('should build loginForm with required email and password fields via initAuthForms()', () => {
      const authForms = component.initAuthForms();
      const loginForm = authForms.loginForm;

      expect(loginForm.valid).toBeFalse();
      loginForm.patchValue({ email: 'user@careeros.com', password: 'password123' });
      expect(loginForm.valid).toBeTrue();
    });

    it('should build registerForm with matching password validation via initAuthForms()', () => {
      const authForms = component.initAuthForms();
      const registerForm = authForms.registerForm;

      registerForm.patchValue({
        fullName: 'User Test',
        email: 'user@careeros.com',
        password: 'Password1!',
        confirmPassword: 'Password2!'
      });
      expect(registerForm.hasError('passwordMismatch')).toBeTrue();

      registerForm.patchValue({ confirmPassword: 'Password1!' });
      expect(registerForm.hasError('passwordMismatch')).toBeFalse();
    });

    it('should initialize resume draft with default template and structure via initCreateResume()', () => {
      const resume = component.initCreateResume();
      expect(resume.title).toBe('Curriculum Vitae');
      expect(resume.template).toBe('modern');
      expect(resume.experiences.length).toBe(0);
    });

    it('should push items to experience, education, and certification lists via App component methods', () => {
      const experiences: any[] = [];
      const educations: any[] = [];
      const certifications: any[] = [];

      component.addExperience(experiences, { company: 'Tech Inc', role: 'Dev' });
      component.addEducation(educations, { institution: 'MIT', course: 'CS' });
      component.addCertification(certifications, { name: 'AWS Certified' });

      expect(experiences.length).toBe(1);
      expect(educations.length).toBe(1);
      expect(certifications.length).toBe(1);
    });

    it('should correctly reorder or swap items in a list via reorderSwap()', () => {
      const list = ['Alpha', 'Beta', 'Gamma'];

      component.reorderSwap(list, 0, 1);
      expect(list).toEqual(['Beta', 'Alpha', 'Gamma']);
    });

    it('should reset user session and profile state on logout()', () => {
      localStorage.setItem('careeros_user_session', JSON.stringify({ token: 'abc' }));
      localStorage.setItem('careeros_profile_draft', JSON.stringify({ draft: true }));

      component.logout();

      expect(localStorage.getItem('careeros_user_session')).toBeNull();
      expect(localStorage.getItem('careeros_profile_draft')).toBeNull();
    });
  });
});
