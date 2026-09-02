import { Component, OnInit, OnDestroy, inject, ChangeDetectorRef } from '@angular/core';
import { FormBuilder, FormGroup, FormArray, Validators, ReactiveFormsModule, FormsModule, AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { AuthSessionService } from './auth/auth-session.service';
import { UserSession } from './auth/auth.models';
import { CandidateProfileService } from './candidate-profile.service';

interface ResumeDto {
  id?: string;
  candidateProfileId: string;
  language: string;
  targetCountry: string;
  showPhone: boolean;
  showEmail: boolean;
  showLocation: boolean;
  customizedTitle: string;
  customizedSummary: string;
  skills: string;
  customizedExperiencesJson?: string;
  customizedEducationsJson?: string;
  customizedCertificationsJson?: string;
}

export const dateRangeValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const start = control.get('startDate')?.value;
  const end = control.get('endDate')?.value;
  const isCurrent = control.get('isCurrent')?.value;

  if (isCurrent) return null;
  if (start && end && new Date(start) > new Date(end)) {
    return { dateRangeInvalid: true };
  }
  return null;
};

export const passwordMatchValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const password = control.get('password')?.value;
  const confirm = control.get('confirmPassword')?.value;
  return password === confirm ? null : { passwordMismatch: true };
};

export const passwordStrengthValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const value: string = control.value ?? '';
  if (!value) return null;
  const hasMinLength = value.length >= 8;
  const hasUpper = /[A-Z]/.test(value);
  const hasLower = /[a-z]/.test(value);
  const hasDigit = /\d/.test(value);
  const hasNonAlphanumeric = /[^a-zA-Z0-9]/.test(value);
  if (hasMinLength && hasUpper && hasLower && hasDigit && hasNonAlphanumeric) {
    return null;
  }
  return { passwordStrength: true };
};

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App implements OnInit, OnDestroy {
  private readonly fb = inject(FormBuilder);
  private readonly http = inject(HttpClient);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly authSession = inject(AuthSessionService);
  private readonly candidateProfileService = inject(CandidateProfileService);

  readonly apiUrl = 'https://localhost:7276/api';

  private readonly authChangedListener = () => this.loadUserSession();

  // Auth State
  currentUser: UserSession | null = null;
  authMode: 'login' | 'register' = 'login';

  // Auth Form Groups
  loginForm!: FormGroup;
  registerForm!: FormGroup;

  // Profile Stepper State
  currentStep = 1;
  candidateId: string | null = null;
  profileForm!: FormGroup;
  resumes: ResumeDto[] = [];

  // LinkedIn Import & Gap Analysis State
  showLinkedinModal = false;
  isImportingLinkedin = false;
  importedLinkedinData: any = null;
  gapAnalysisData: any = null;
  activeProfileTab: 'profile' | 'todo' = 'profile';

  // Active editing resume state
  editingResume: ResumeDto | null = null;
  showResumeForm = false;
  resumeLanguages = [
    { code: 'pt', name: 'Português', country: 'BR' },
    { code: 'en', name: 'English', country: 'US' },
    { code: 'it', name: 'Italiano', country: 'IT' }
  ];

  // Feedback Messages
  statusMessage = '';
  isSuccess = true;

  ngOnInit() {
    this.initAuthForms();
    this.initForm();
    this.loadUserSession();
    window.addEventListener('careeros-auth-changed', this.authChangedListener);
  }

  ngOnDestroy() {
    window.removeEventListener('careeros-auth-changed', this.authChangedListener);
  }

  private initAuthForms() {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]]
    });

    this.registerForm = this.fb.group({
      email: ['', [Validators.required, Validators.email, Validators.maxLength(320)]],
      password: ['', [Validators.required, passwordStrengthValidator, Validators.maxLength(100)]],
      confirmPassword: ['', [Validators.required]],
      fullName: ['', [Validators.required, Validators.maxLength(200)]],
      professionalTitle: ['', [Validators.required, Validators.maxLength(160)]]
    }, { validators: passwordMatchValidator });
  }

  private initForm() {
    this.profileForm = this.fb.group({
      fullName: ['', [Validators.required, Validators.maxLength(200)]],
      preferredName: ['', [Validators.maxLength(200)]],
      professionalTitle: ['', [Validators.required, Validators.maxLength(160)]],
      professionalSummary: ['', [Validators.maxLength(4000)]],
      email: ['', [Validators.required, Validators.email, Validators.maxLength(320)]],
      phone: ['', [Validators.pattern(/^\+?[0-9\s\-()]{8,25}$/)]],
      city: [''],
      region: [''],
      country: [''],
      openToRemoteWork: [false],
      openToRelocation: [false],
      experiences: this.fb.array([]),
      educations: this.fb.array([]),
      certifications: this.fb.array([])
    });

    // Save draft automatically on form value changes when logged in
    this.profileForm.valueChanges.subscribe(() => {
      if (this.currentUser) {
        this.saveDraft();
      }
    });
  }

  // Getters for form arrays
  get experiencesArray(): FormArray {
    return this.profileForm.get('experiences') as FormArray;
  }

  get educationsArray(): FormArray {
    return this.profileForm.get('educations') as FormArray;
  }

  get certificationsArray(): FormArray {
    return this.profileForm.get('certifications') as FormArray;
  }

  // Auth Operations
  login() {
    if (this.loginForm.invalid) {
      this.showStatus('Preencha os campos corretamente para efetuar o login.', false);
      return;
    }

    const payload = this.loginForm.value;
    this.http.post<UserSession>(`${this.apiUrl}/auth/login`, payload).subscribe({
      next: (session) => {
        this.setupUserSession(session);
        this.showStatus(`Bem-vindo(a) de volta, ${session.fullName}!`, true);
      },
      error: (err) => {
        console.error('Login error', err);
        const errMsg = err.error?.message || 'Falha na autenticação. Verifique seu e-mail e senha.';
        this.showStatus(errMsg, false);
      }
    });
  }

  loginWithGoogle() {
    this.http.get<{ url: string }>(`${this.apiUrl}/auth/login-google`).subscribe({
      next: (res) => {
        if (res?.url) {
          window.location.assign(res.url);
        } else {
          window.location.assign(`${this.apiUrl}/auth/login-google-complete`);
        }
      },
      error: () => {
        window.location.assign(`${this.apiUrl}/auth/login-google-complete`);
      }
    });
  }

  register() {
    if (this.registerForm.invalid) {
      this.showStatus('Por favor, preencha o formulário de cadastro corretamente.', false);
      return;
    }

    const payload = {
      email: this.registerForm.value.email,
      password: this.registerForm.value.password,
      fullName: this.registerForm.value.fullName,
      professionalTitle: this.registerForm.value.professionalTitle
    };

    this.http.post<UserSession>(`${this.apiUrl}/auth/register`, payload).subscribe({
      next: (session) => {
        this.setupUserSession(session);
        this.showStatus(`Cadastro concluído com sucesso, ${session.fullName}!`, true);
      },
      error: (err) => {
        console.error('Registration error', err);
        const errMsg = err.error?.message || 'Não foi possível criar a conta. Tente novamente em instantes.';
        this.showStatus(errMsg, false);
      }
    });
  }

  logout() {
    this.authSession.clearSession();
    this.currentUser = null;
    this.candidateId = null;
    this.profileForm.reset();
    this.experiencesArray.clear();
    this.educationsArray.clear();
    this.certificationsArray.clear();
    this.resumes = [];
    this.currentStep = 1;
    this.loginForm.reset();
    this.registerForm.reset();
    this.showStatus('Sessão encerrada com sucesso.', true);
    this.cdr.detectChanges();
  }

  private setupUserSession(session: UserSession) {
    this.currentUser = session;
    this.candidateId = session.candidateProfileId;
    this.authSession.setSession(session);
    this.loadProfileFromApi(session.candidateProfileId);
    this.loadResumes(session.candidateProfileId);
    this.cdr.detectChanges();
  }

  private loadUserSession() {
    const session = this.authSession.getSession();
    if (session) {
      this.currentUser = session;
      this.candidateId = session.candidateProfileId;
      this.loadDraftOrNew();
      this.loadProfileFromApi(session.candidateProfileId);
      this.loadResumes(session.candidateProfileId);
      this.cdr.detectChanges();
    }
  }

  // List manipulation methods
  addExperience(data?: any) {
    const expForm = this.fb.group({
      id: [data?.id || null],
      companyName: [data?.companyName || '', [Validators.required, Validators.maxLength(200)]],
      jobTitle: [data?.jobTitle || '', [Validators.required, Validators.maxLength(160)]],
      description: [data?.description || '', [Validators.maxLength(4000)]],
      startDate: [data?.startDate ? this.formatDate(data.startDate) : '', Validators.required],
      endDate: [data?.endDate ? this.formatDate(data.endDate) : ''],
      isCurrent: [data?.isCurrent || false],
      order: [data?.order ?? this.experiencesArray.length]
    }, { validators: dateRangeValidator });
    this.experiencesArray.push(expForm);
    this.cdr.detectChanges();
  }

  removeExperience(index: number) {
    this.experiencesArray.removeAt(index);
    this.reorderArray(this.experiencesArray);
    this.showStatus('Experiência removida.', true);
  }

  swapExperiences(i1: number, i2: number) {
    if (i1 < 0 || i1 >= this.experiencesArray.length || i2 < 0 || i2 >= this.experiencesArray.length) return;
    const temp = this.experiencesArray.at(i1).value;
    this.experiencesArray.at(i1).patchValue(this.experiencesArray.at(i2).value);
    this.experiencesArray.at(i2).patchValue(temp);
    this.reorderArray(this.experiencesArray);
    this.showStatus('Experiência reordenada.', true);
  }

  addEducation(data?: any) {
    const eduForm = this.fb.group({
      id: [data?.id || null],
      institution: [data?.institution || '', [Validators.required, Validators.maxLength(200)]],
      degree: [data?.degree || '', [Validators.required, Validators.maxLength(100)]],
      fieldOfStudy: [data?.fieldOfStudy || '', [Validators.required, Validators.maxLength(100)]],
      startDate: [data?.startDate ? this.formatDate(data.startDate) : '', Validators.required],
      endDate: [data?.endDate ? this.formatDate(data.endDate) : ''],
      isCurrent: [data?.isCurrent || false],
      order: [data?.order ?? this.educationsArray.length]
    }, { validators: dateRangeValidator });
    this.educationsArray.push(eduForm);
    this.cdr.detectChanges();
  }

  removeEducation(index: number) {
    this.educationsArray.removeAt(index);
    this.reorderArray(this.educationsArray);
    this.showStatus('Formação removida.', true);
  }

  swapEducations(i1: number, i2: number) {
    if (i1 < 0 || i1 >= this.educationsArray.length || i2 < 0 || i2 >= this.educationsArray.length) return;
    const temp = this.educationsArray.at(i1).value;
    this.educationsArray.at(i1).patchValue(this.educationsArray.at(i2).value);
    this.educationsArray.at(i2).patchValue(temp);
    this.reorderArray(this.educationsArray);
    this.showStatus('Formação reordenada.', true);
  }

  addCertification(data?: any) {
    const certForm = this.fb.group({
      id: [data?.id || null],
      name: [data?.name || '', [Validators.required, Validators.maxLength(200)]],
      issuingOrganization: [data?.issuingOrganization || '', [Validators.required, Validators.maxLength(200)]],
      issueDate: [data?.issueDate ? this.formatDate(data.issueDate) : ''],
      expirationDate: [data?.expirationDate ? this.formatDate(data.expirationDate) : ''],
      credentialId: [data?.credentialId || ''],
      credentialUrl: [data?.credentialUrl || ''],
      order: [data?.order ?? this.certificationsArray.length]
    });
    this.certificationsArray.push(certForm);
    this.cdr.detectChanges();
  }

  removeCertification(index: number) {
    this.certificationsArray.removeAt(index);
    this.reorderArray(this.certificationsArray);
    this.showStatus('Certificação removida.', true);
  }

  swapCertifications(i1: number, i2: number) {
    if (i1 < 0 || i1 >= this.certificationsArray.length || i2 < 0 || i2 >= this.certificationsArray.length) return;
    const temp = this.certificationsArray.at(i1).value;
    this.certificationsArray.at(i1).patchValue(this.certificationsArray.at(i2).value);
    this.certificationsArray.at(i2).patchValue(temp);
    this.reorderArray(this.certificationsArray);
    this.showStatus('Certificação reordenada.', true);
  }

  private reorderArray(arr: FormArray) {
    for (let i = 0; i < arr.length; i++) {
      arr.at(i).get('order')?.setValue(i);
    }
    this.saveDraft();
    this.cdr.detectChanges();
  }

  // Stepper navigation
  nextStep() {
    if (this.currentStep < 5) {
      this.currentStep++;
      this.cdr.detectChanges();
    }
  }

  prevStep() {
    if (this.currentStep > 1) {
      this.currentStep--;
      this.cdr.detectChanges();
    }
  }

  // Draft persistence in localStorage
  saveDraft() {
    const draft = {
      candidateId: this.candidateId,
      formData: this.profileForm.value
    };
    localStorage.setItem('careeros_profile_draft', JSON.stringify(draft));
  }

  private loadDraftOrNew() {
    const cached = localStorage.getItem('careeros_profile_draft');
    if (cached) {
      try {
        const draft = JSON.parse(cached);
        this.candidateId = draft.candidateId || this.candidateId;
        if (draft.formData) {
          this.patchForm(draft.formData);
        }
      } catch (e) {
        console.error('Falha ao ler rascunho', e);
      }
    }
  }

  private patchForm(data: any) {
    // Clear and build arrays
    this.experiencesArray.clear();
    this.educationsArray.clear();
    this.certificationsArray.clear();

    const experiences = data.experiences ?? data.workExperiences ?? [];
    experiences.forEach((exp: any) => this.addExperience({
      id: exp.id,
      companyName: exp.company || exp.companyName,
      jobTitle: exp.role || exp.jobTitle,
      description: exp.description,
      startDate: exp.startDate,
      endDate: exp.endDate,
      isCurrent: exp.isCurrent,
      order: exp.displayOrder ?? exp.order
    }));
    const educations = data.educations ?? data.educationHistory ?? [];
    educations.forEach((edu: any) => this.addEducation({
      id: edu.id,
      institution: edu.institution,
      degree: edu.degree,
      fieldOfStudy: edu.course || edu.fieldOfStudy,
      startDate: edu.startDate,
      endDate: edu.completionDate || edu.endDate,
      isCurrent: edu.isCurrent,
      order: edu.displayOrder ?? edu.order
    }));
    const certifications = data.certifications ?? [];
    certifications.forEach((cert: any) => this.addCertification({
      id: cert.id,
      name: cert.name,
      issuingOrganization: cert.issuer || cert.issuingOrganization,
      issueDate: cert.issuedAt || cert.issueDate,
      credentialUrl: cert.credentialUrl,
      order: cert.displayOrder ?? cert.order
    }));

    this.profileForm.patchValue({
      fullName: data.fullName || '',
      preferredName: data.preferredName || '',
      professionalTitle: data.professionalTitle || '',
      professionalSummary: data.professionalSummary || '',
      email: data.email || '',
      phone: data.phone || '',
      city: data.city || '',
      region: data.region || '',
      country: data.country || '',
      openToRemoteWork: !!data.openToRemoteWork,
      openToRelocation: !!data.openToRelocation
    }, { emitEvent: false });
    this.cdr.detectChanges();
  }

  // Backend Integration
  private loadProfileFromApi(id: string) {
    this.http.get<any>(`${this.apiUrl}/candidate-profiles/${id}`).subscribe({
      next: (profile) => {
        if (profile) {
          this.patchForm(profile);
          this.saveDraft();
        }
      },
      error: (err) => console.error('Erro ao carregar perfil do backend', err)
    });
  }

  saveProfileToApi() {
    if (this.profileForm.invalid) {
      this.showStatus('Formulário possui campos inválidos. Por favor, verifique.', false);
      return;
    }

    const v = this.profileForm.value;
    const payload = {
      fullName: v.fullName,
      preferredName: v.preferredName,
      professionalTitle: v.professionalTitle,
      professionalSummary: v.professionalSummary,
      email: v.email,
      phone: v.phone,
      city: v.city,
      region: v.region,
      country: v.country,
      openToRemoteWork: !!v.openToRemoteWork,
      openToRelocation: !!v.openToRelocation,
      workExperiences: (v.experiences ?? []).map((x: any) => ({
        company: x.companyName,
        role: x.jobTitle,
        startDate: x.startDate || null,
        endDate: x.isCurrent ? null : (x.endDate || null),
        isCurrent: !!x.isCurrent,
        description: x.description,
        displayOrder: x.order
      })),
      educationHistory: (v.educations ?? []).map((x: any) => ({
        institution: x.institution,
        course: x.fieldOfStudy,
        degree: x.degree,
        completionDate: x.isCurrent ? null : (x.endDate || null),
        displayOrder: x.order
      })),
      certifications: (v.certifications ?? []).map((x: any) => ({
        name: x.name,
        issuer: x.issuingOrganization,
        issuedAt: x.issueDate || null,
        credentialUrl: x.credentialUrl,
        displayOrder: x.order
      }))
    };
    this.candidateProfileService.save(this.candidateId, payload).subscribe({
      next: (res: any) => {
        if (res?.id) {
          this.candidateId = res.id;
        }
        this.saveDraft();
        this.showStatus('Perfil salvo na nuvem com sucesso!', true);
        if (this.candidateId) {
          this.loadResumes(this.candidateId);
        }
      },
      error: (err) => {
        console.error('Erro ao salvar perfil', err);
        this.showStatus('Falha ao salvar o perfil.', false);
      }
    });
  }

  deleteProfile() {
    if (!this.candidateId) return;
    if (confirm('Tem certeza de que deseja excluir o perfil e todos os currículos associados permanentemente?')) {
      this.http.delete(`${this.apiUrl}/candidate-profiles/${this.candidateId}`).subscribe({
        next: () => {
          this.logout();
          this.showStatus('Perfil excluído permanentemente.', true);
        },
        error: (err) => {
          console.error(err);
          this.showStatus('Falha ao excluir o perfil.', false);
        }
      });
    }
  }

  // Resume Management
  loadResumes(candidateId: string) {
    this.http.get<ResumeDto[]>(`${this.apiUrl}/resumes/by-candidate/${candidateId}`).subscribe({
      next: (res) => {
        this.resumes = res;
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Falha ao carregar currículos', err)
    });
  }

  initCreateResume() {
    if (!this.candidateId) {
      this.showStatus('Salve o perfil profissional primeiro.', false);
      return;
    }
    this.editingResume = {
      candidateProfileId: this.candidateId,
      language: 'pt',
      targetCountry: 'BR',
      showPhone: true,
      showEmail: true,
      showLocation: true,
      customizedTitle: this.profileForm.value.professionalTitle || '',
      customizedSummary: this.profileForm.value.professionalSummary || '',
      skills: ''
    };
    this.showResumeForm = true;
    this.cdr.detectChanges();
  }

  editResume(resume: ResumeDto) {
    this.editingResume = { ...resume };
    this.showResumeForm = true;
    this.cdr.detectChanges();
  }

  saveResume() {
    if (!this.editingResume) return;

    if (!this.editingResume.customizedTitle || !this.editingResume.skills) {
      this.showStatus('Título e Competências são campos obrigatórios para o Currículo.', false);
      return;
    }

    const request = this.editingResume.id
      ? this.http.put<ResumeDto>(`${this.apiUrl}/resumes/${this.editingResume.id}`, this.editingResume)
      : this.http.post<ResumeDto>(`${this.apiUrl}/resumes`, this.editingResume);

    request.subscribe({
      next: () => {
        this.showResumeForm = false;
        this.editingResume = null;
        this.showStatus('Currículo salvo com sucesso!', true);
        if (this.candidateId) {
          this.loadResumes(this.candidateId);
        }
      },
      error: (err) => {
        console.error(err);
        this.showStatus('Falha ao salvar currículo.', false);
      }
    });
  }

  deleteResume(id: string) {
    if (confirm('Excluir este currículo permanentemente?')) {
      this.http.delete(`${this.apiUrl}/resumes/${id}`).subscribe({
        next: () => {
          this.showStatus('Currículo excluído.', true);
          if (this.candidateId) {
            this.loadResumes(this.candidateId);
          }
        },
        error: (err) => console.error(err)
      });
    }
  }

  exportResume(id: string, format: 'pdf' | 'docx' | 'ats') {
    const url = `${this.apiUrl}/resumes/${id}/export/${format}`;
    if (format === 'ats') {
      this.http.get(url, { responseType: 'text' }).subscribe({
        next: (text) => {
          const blob = new Blob([text], { type: 'text/plain;charset=utf-8' });
          const link = document.createElement('a');
          link.href = URL.createObjectURL(blob);
          link.download = `curriculo_ats.txt`;
          link.click();
        },
        error: (err) => console.error(err)
      });
    } else {
      this.http.get(url, { responseType: 'blob' }).subscribe({
        next: (blob) => {
          const link = document.createElement('a');
          link.href = URL.createObjectURL(blob);
          link.download = `curriculo.${format}`;
          link.click();
        },
        error: (err) => console.error(err)
      });
    }
  }

  // LinkedIn Import Methods
  onLinkedinFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;

    const file = input.files[0];
    if (!file.name.toLowerCase().endsWith('.pdf')) {
      this.showStatus('Por favor, selecione um arquivo PDF exportado do LinkedIn.', false);
      return;
    }

    this.isImportingLinkedin = true;
    this.cdr.detectChanges();

    this.candidateProfileService.importLinkedin(file).subscribe({
      next: (res) => {
        this.isImportingLinkedin = false;
        this.importedLinkedinData = res.parsedProfile;
        this.gapAnalysisData = res.gapAnalysis;
        this.showLinkedinModal = true;
        this.showStatus('PDF do LinkedIn analisado com sucesso!', true);
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isImportingLinkedin = false;
        console.error('Erro na importação do LinkedIn', err);
        const msg = err.error?.message || 'Não foi possível ler o arquivo PDF do LinkedIn.';
        this.showStatus(msg, false);
        this.cdr.detectChanges();
      }
    });
  }

  applyLinkedinField(field: string) {
    if (!this.importedLinkedinData) return;
    const val = this.importedLinkedinData[field];
    if (val !== undefined && val !== null) {
      this.profileForm.get(field)?.setValue(val);
      this.saveDraft();
      this.showStatus(`Campo ${field} atualizado com dados do LinkedIn.`, true);
      this.cdr.detectChanges();
    }
  }

  mergeAllLinkedinData() {
    if (!this.importedLinkedinData) return;

    const d = this.importedLinkedinData;

    if (d.fullName) this.profileForm.get('fullName')?.setValue(d.fullName);
    if (d.professionalTitle) this.profileForm.get('professionalTitle')?.setValue(d.professionalTitle);
    if (d.professionalSummary) this.profileForm.get('professionalSummary')?.setValue(d.professionalSummary);
    if (d.email) this.profileForm.get('email')?.setValue(d.email);
    if (d.phone) this.profileForm.get('phone')?.setValue(d.phone);
    if (d.city) this.profileForm.get('city')?.setValue(d.city);
    if (d.region) this.profileForm.get('region')?.setValue(d.region);
    if (d.country) this.profileForm.get('country')?.setValue(d.country);

    if (d.workExperiences && d.workExperiences.length > 0) {
      this.experiencesArray.clear();
      d.workExperiences.forEach((exp: any) => {
        this.addExperience({
          companyName: exp.company,
          jobTitle: exp.role,
          description: exp.description,
          startDate: exp.startDate,
          endDate: exp.endDate,
          isCurrent: exp.isCurrent,
          order: exp.displayOrder
        });
      });
    }

    if (d.educationHistory && d.educationHistory.length > 0) {
      this.educationsArray.clear();
      d.educationHistory.forEach((edu: any) => {
        this.addEducation({
          institution: edu.institution,
          fieldOfStudy: edu.course,
          degree: edu.degree || 'Bacharelado',
          startDate: edu.startDate || '2020-01-01',
          endDate: edu.completionDate,
          order: edu.displayOrder
        });
      });
    }

    if (d.certifications && d.certifications.length > 0) {
      this.certificationsArray.clear();
      d.certifications.forEach((cert: any) => {
        this.addCertification({
          name: cert.name,
          issuingOrganization: cert.issuer || 'LinkedIn',
          issueDate: cert.issuedAt,
          credentialUrl: cert.credentialUrl,
          order: cert.displayOrder
        });
      });
    }

    this.saveDraft();
    this.showLinkedinModal = false;
    this.showStatus('Todos os dados do LinkedIn foram mesclados ao seu perfil!', true);
    this.cdr.detectChanges();
  }

  // Helpers
  private formatDate(dateVal: any): string {
    if (!dateVal) return '';
    try {
      const d = new Date(dateVal);
      const year = d.getFullYear();
      const month = String(d.getMonth() + 1).padStart(2, '0');
      const day = String(d.getDate()).padStart(2, '0');
      return `${year}-${month}-${day}`;
    } catch {
      return '';
    }
  }

  private showStatus(msg: string, success: boolean) {
    this.statusMessage = msg;
    this.isSuccess = success;
    this.cdr.detectChanges();
    setTimeout(() => {
      if (this.statusMessage === msg) {
        this.statusMessage = '';
        this.cdr.detectChanges();
      }
    }, 4000);
  }
}
