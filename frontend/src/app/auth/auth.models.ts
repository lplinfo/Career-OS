export interface UserSession {
  userId: string;
  email: string;
  candidateProfileId: string;
  fullName: string;
  accessToken: string;
  tokenType: string;
  expiresAt: string;
}
