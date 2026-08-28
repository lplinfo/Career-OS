import { Routes } from '@angular/router';
import { GoogleCallbackComponent } from './auth/google-callback.component';

export const routes: Routes = [
  { path: 'auth/callback', component: GoogleCallbackComponent }
];
