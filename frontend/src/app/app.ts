import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CandidateProfileService } from './candidate-profile.service';

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

  protected next(): void { if (this.step() < this.steps.length - 1) this.step.update(value => value + 1); }
  protected previous(): void { if (this.step() > 0) this.step.update(value => value - 1); }
  protected saveDraft(): void {
    localStorage.setItem('careeros-candidate-draft', JSON.stringify(this.form.getRawValue()));
    this.savedAt.set(new Date().toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' }));
  }

  protected saveProfile(): void {
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
}
