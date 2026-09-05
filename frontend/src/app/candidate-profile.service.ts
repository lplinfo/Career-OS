import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../environments/environment';

@Injectable({ providedIn: 'root' })
export class CandidateProfileService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/candidate-profiles`;

  save(id: string | null, profile: unknown) {
    return id ? this.http.put(`${this.url}/${id}`, profile) : this.http.post<{ id: string }>(this.url, profile);
  }

  importLinkedin(file: File) {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<any>(`${this.url}/import-linkedin`, formData);
  }
}
