import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { environment } from '../../environments/environment';
import { UserProfile } from '../../shared/models/user';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class MemberService {

  private http = inject(HttpClient)
  private apiUrl = environment.apiUrl

  members = signal<UserProfile[]>([]);

  getMembers() {
    this.http.get<UserProfile[]>(`${this.apiUrl}Members`).subscribe({
      next: res => {
        this.members.set(res);
      },
      error: err => {
        console.log(err)
      }
    })
  }

  getMemberDetail(userId: string): Observable<UserProfile> {
    return this.http.post<UserProfile>(`${this.apiUrl}Members/detail`, { memberId: userId });
  }

  redeemReward(points: number) {
  return this.http.put<string>(`${this.apiUrl}Members/redeem-reward`, points);
}

}
