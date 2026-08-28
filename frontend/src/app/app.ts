import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators, ValidatorFn, AbstractControl, ValidationErrors } from '@angular/forms';
import { CandidateProfileService } from './candidate-profile.service';

export const dateRangeValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const startDate = control.get('startDate')?.value;
  const endDate = control.get('endDate')?.value;
  const isCurrent = control.get('isCurrent')?.value;

  if (isCurrent) {
    return null;
  }
  if (startDate && endDate && new Date(endDate) < new Date(startDate)) {
    return { dateRangeInvalid: true };
  }
  return null;
};

export const passwordMatchValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const password = control.get('password')?.value;
  const confirmPassword = control.get('confirmPassword')?.value;
  if (password && confirmPassword && password !== confirmPassword) {
    return { passwordMismatch: true };
  }
  return null;
};

@Component({
  selector: 'app-root',
  imports: [ReactiveFormsModule],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly profileService = inject(CandidateProfileService);
  protected readonly step = signal(0);
  protected readonly steps = ['Perfil', 'Experiência', 'Formação', 'Competências'];
  protected savedAt = signal<string | null>(null);
  protected saving = signal(false);
  protected saveMessage = signal<string | null>(null);

  protected readonly form = this.fb.group({
    profile: this.fb.group({
      fullName: ['', [Validators.required, Validators.maxLength(200)]],
      professionalTitle: ['', [Validators.required, Validators.maxLength(160)]],
      email: ['', [Validators.required, Validators.email]],
      phone: [''], city: [''], country: [''],
      professionalSummary: ['', Validators.maxLength(4000)],
      openToRemoteWork: [false], openToRelocation: [false]
    }),
    experience: this.fb.group({ company: [''], role: [''], startDate: [''], endDate: [''], description: [''] }),
    education: this.fb.group({ institution: [''], course: [''], degree: [''], completionDate: [''] }),
    skills: this.fb.group({ languages: [''], technicalSkills: [''], certifications: [''] })
  });

  ngOnInit(): void {
    const draft = localStorage.getItem('careeros-candidate-draft');
    if (draft) this.form.patchValue(JSON.parse(draft));
    this.form.valueChanges.subscribe(() => this.saveDraft());
  }

  public next(): void { if (this.step() < this.steps.length - 1) this.step.update(value => value + 1); }
  public previous(): void { if (this.step() > 0) this.step.update(value => value - 1); }
  public saveDraft(): void {
    localStorage.setItem('careeros-candidate-draft', JSON.stringify(this.form.getRawValue()));
    this.savedAt.set(new Date().toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' }));
  }

  public saveProfile(): void {
    if (this.form.invalid) { this.step.set(0); this.form.markAllAsTouched(); return; }
    const value = this.form.getRawValue();
    const profileId = localStorage.getItem('careeros-profile-id');
    const payload = {
      ...value.profile,
      workExperiences: value.experience.company ? [{ company: value.experience.company, role: value.experience.role ?? '', startDate: value.experience.startDate || null, endDate: value.experience.endDate || null, isCurrent: !value.experience.endDate, description: value.experience.description, displayOrder: 0 }] : [],
      educationHistory: value.education.institution ? [{ institution: value.education.institution, course: value.education.course ?? '', degree: value.education.degree, completionDate: value.education.completionDate || null, displayOrder: 0 }] : [],
      certifications: value.skills.certifications ? value.skills.certifications.split('\n').filter(Boolean).map((name, index) => ({ name, issuer: null, issuedAt: null, credentialUrl: null, displayOrder: index })) : []
    };
    this.saving.set(true); this.saveMessage.set(null);
    this.profileService.save(profileId, payload).subscribe({
      next: (response: any) => { if (response?.id) localStorage.setItem('careeros-profile-id', response.id); this.saving.set(false); this.saveMessage.set('Perfil salvo na API.'); },
      error: () => { this.saving.set(false); this.saveMessage.set('Não foi possível salvar. Confirme se a API está em execução.'); }
    });
  }

  public initAuthForms() {
    return {
      loginForm: this.fb.group({
        email: ['', [Validators.required, Validators.email]],
        password: ['', [Validators.required]]
      }),
      registerForm: this.fb.group({
        fullName: ['', Validators.required],
        email: ['', [Validators.required, Validators.email]],
        password: ['', [Validators.required, Validators.minLength(6)]],
        confirmPassword: ['', Validators.required]
      }, { validators: [passwordMatchValidator] })
    };
  }

  public initCreateResume() {
    return {
      title: 'Curriculum Vitae',
      template: 'modern',
      experiences: [] as any[],
      educations: [] as any[],
      certifications: [] as any[]
    };
  }

  public addExperience(list: any[], exp: any) {
    list.push(exp);
  }

  public addEducation(list: any[], edu: any) {
    list.push(edu);
  }

  public addCertification(list: any[], cert: any) {
    list.push(cert);
  }

  public reorderSwap(list: any[], indexA: number, indexB: number) {
    if (indexA >= 0 && indexB >= 0 && indexA < list.length && indexB < list.length) {
      const temp = list[indexA];
      list[indexA] = list[indexB];
      list[indexB] = temp;
    }
  }

  public logout() {
    localStorage.removeItem('careeros_user_session');
    localStorage.removeItem('careeros_profile_draft');
  }
}
