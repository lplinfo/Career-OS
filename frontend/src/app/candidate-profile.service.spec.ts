import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { CandidateProfileService } from './candidate-profile.service';
import { environment } from '../environments/environment';

describe('CandidateProfileService', () => {
  let service: CandidateProfileService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        CandidateProfileService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(CandidateProfileService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should send POST request when saving profile with null id', () => {
    const mockProfile = { fullName: 'John Doe', email: 'john@example.com' };
    const mockResponse = { id: '12345' };

    service.save(null, mockProfile).subscribe((res) => {
      expect(res).toEqual(mockResponse);
    });

    const req = httpMock.expectOne(`${environment.apiUrl}/candidate-profiles`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(mockProfile);
    req.flush(mockResponse);
  });

  it('should send PUT request when saving profile with existing id', () => {
    const profileId = '12345';
    const mockProfile = { fullName: 'Jane Doe', email: 'jane@example.com' };

    service.save(profileId, mockProfile).subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/candidate-profiles/${profileId}`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(mockProfile);
    req.flush({});
  });
});
