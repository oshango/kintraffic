import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { AppUser } from '../models/user.model';

@Injectable({ providedIn: 'root' })
export class SecurityService {
  constructor(private readonly http: HttpClient) {}

  getCurrentUser(): Observable<AppUser> {
    return this.http.get<AppUser>('/api/auth/current-user');
  }
}
