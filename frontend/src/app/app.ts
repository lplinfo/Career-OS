import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

@Component({
  selector: 'app-root',
  imports: [ReactiveFormsModule],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App implements OnInit {
  private readonly fb = inject(FormBuilder);
  protected readonly step = signal(0);
  protected readonly steps = ['Perfil', 'Experiência', 'Formação', 'Competências'];
  protected savedAt = signal<string | null>(null);

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
}
