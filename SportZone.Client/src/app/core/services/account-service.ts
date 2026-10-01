import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { environment } from '../../environments/environment';
import { authenResponse, authenLoginCreds, RegisterCreds, User, UserProfile, VerifyOtpCreds } from '../../shared/models/user';
import { tap } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class AccountService {
  
  private http = inject(HttpClient);
  currentUser = signal<User | null>(null);
  baseUrl = environment.apiUrl;
  
  // register(creds: RegisterCreds) {
  //   return this.http.post<User>(this.baseUrl + 'account/register', creds,
  //     { withCredentials: true }).pipe(
  //       tap(user => {
  //         if (user) {
  //           this.setCurrentUser(user);
  //           // this.startTokenRefreshInterval();
  //         }
  //       })
  //     )
  // }

 authenticate(creds: authenLoginCreds) {
    return this.http.post<authenResponse>(this.baseUrl + 'account/authenticate', creds, { withCredentials: true })
      .pipe(
        tap(res => {
          // Chỉ set User khi Backend trả về user (tức là đăng nhập thành công thẳng luôn)
          if (res.user && !res.requireOtp) {
            this.setCurrentUser(res.user);
            this.startTokenRefreshInterval();
          }
        })
      );
  }

  verifyEmail(creds: VerifyOtpCreds) {
    return this.http.post<authenResponse>(this.baseUrl + 'account/verify-email', creds, { withCredentials: true })
      .pipe(
        tap(res => {
          // Xác thực thành công -> Backend trả về Token -> Set User luôn
          if (res.user) {
            this.setCurrentUser(res.user);
            this.startTokenRefreshInterval();
          }
        })
      );
  }

  refreshToken() {
    return this.http.post<User>(this.baseUrl + 'account/refresh-token', {},
      { withCredentials: true })
  }

  getProfile() {
    return this.http.get<UserProfile>(this.baseUrl + 'account/profile')
    }


  logout() {
    this.http.post(this.baseUrl + 'account/logout', {}, { withCredentials: true }).subscribe({
      next: () => {
        this.currentUser.set(null);
         window.location.href = '/';
      }
    })

  }

  updateProfile(profile: UserProfile) {
    return this.http.put<UserProfile>(this.baseUrl + 'account/profile', profile)
  }

  startTokenRefreshInterval() {
    setInterval(() => {
      this.http.post<User>(this.baseUrl + 'account/refresh-token', {},
        { withCredentials: true }).subscribe({
          next: user => {
            this.setCurrentUser(user)
          },
          error: () => {
            this.logout()
          }
        })
    }, 14 * 24 * 60 * 60 * 1000) // 14 days
  }

  setCurrentUser(user: User) {
    user.roles = this.getRolesFromToken(user);
    this.currentUser.set(user);
  }


  private getRolesFromToken(user: User): string[] {
    const payload = user.token.split('.')[1];
    const decoded = atob(payload);
    const jsonPayload = JSON.parse(decoded);
    return Array.isArray(jsonPayload.role) ? jsonPayload.role : [jsonPayload.role]
  }

 
}