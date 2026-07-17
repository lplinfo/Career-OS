import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, FormGroup, FormArray, Validators, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';

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

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly http = inject(HttpClient);

  readonly apiUrl = 'http://localhost:5062/api';

  // State
  currentStep = 1;
  candidateId: string | null = null;
  profileForm!: FormGroup;
  resumes: ResumeDto[] = [];

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
    this.initForm();
    this.loadDraftOrNew();
    if (this.candidateId) {
      this.loadProfileFromApi(this.candidateId);
      this.loadResumes(this.candidateId);
    }
  }

  private initForm() {
    this.profileForm = this.fb.group({
      fullName: ['', [Validators.required, Validators.maxLength(200)]],
      preferredName: ['', [Validators.maxLength(200)]],
      professionalTitle: ['', [Validators.required, Validators.maxLength(160)]],
      professionalSummary: ['', [Validators.maxLength(4000)]],
      email: ['', [Validators.required, Validators.email, Validators.maxLength(320)]],
      phone: [''],
      city: [''],
      region: [''],
      country: [''],
      openToRemoteWork: [false],
      openToRelocation: [false],
      experiences: this.fb.array([]),
      educations: this.fb.array([]),
      certifications: this.fb.array([])
    });

    // Save draft automatically on form value changes
    this.profileForm.valueChanges.subscribe(() => {
      this.saveDraft();
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
    });
    this.experiencesArray.push(expForm);
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
    });
    this.educationsArray.push(eduForm);
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
  }

  // Stepper navigation
  nextStep() {
    if (this.currentStep < 5) {
      this.currentStep++;
    }
  }

  prevStep() {
    if (this.currentStep > 1) {
      this.currentStep--;
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
        this.candidateId = draft.candidateId || null;
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

    if (data.experiences) {
      data.experiences.forEach((exp: any) => this.addExperience(exp));
    }
    if (data.educations) {
      data.educations.forEach((edu: any) => this.addEducation(edu));
    }
    if (data.certifications) {
      data.certifications.forEach((cert: any) => this.addCertification(cert));
    }

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

    const payload = this.profileForm.value;
    const request = this.candidateId
      ? this.http.put<any>(`${this.apiUrl}/candidate-profiles/${this.candidateId}`, payload)
      : this.http.post<any>(`${this.apiUrl}/candidate-profiles`, payload);

    request.subscribe({
      next: (res) => {
        this.candidateId = res.id;
        this.saveDraft();
        this.showStatus('Perfil salvo na nuvem com sucesso!', true);
        this.loadResumes(res.id);
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
          localStorage.removeItem('careeros_profile_draft');
          this.candidateId = null;
          this.profileForm.reset();
          this.experiencesArray.clear();
          this.educationsArray.clear();
          this.certificationsArray.clear();
          this.resumes = [];
          this.currentStep = 1;
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
      next: (res) => this.resumes = res,
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
  }

  editResume(resume: ResumeDto) {
    this.editingResume = { ...resume };
    this.showResumeForm = true;
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
      // For ATS plain text, we can open it in a new window or trigger download
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
      // Binary downloads (PDF / DOCX)
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
    setTimeout(() => {
      if (this.statusMessage === msg) {
        this.statusMessage = '';
      }
    }, 4000);
  }
}
