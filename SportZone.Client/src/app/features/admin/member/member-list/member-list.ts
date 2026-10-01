import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { UserProfile } from '../../../../shared/models/user';
import { MemberService } from '../../../../core/services/member-service';

@Component({
  selector: 'app-member-list',
  standalone: true, // Đảm bảo có standalone nếu dùng Angular mới
  imports: [CommonModule],
  templateUrl: './member-list.html',
  styleUrl: './member-list.css',
})
export class MemberList implements OnInit {
  private memberService = inject(MemberService);

  members = this.memberService.members;
  selectedMember = signal<UserProfile | null>(null);
  isLoadingDetail = signal(false);
  expandedOrderId = signal<number | null>(null);

  ngOnInit(): void {
    this.memberService.getMembers();
  }

  selectMember(userId: string) {
    this.isLoadingDetail.set(true);
    this.memberService.getMemberDetail(userId).subscribe({
      next: (res) => {
        this.selectedMember.set(res);
        this.isLoadingDetail.set(false);
      },
      error: (err) => {
        console.error(err);
        this.isLoadingDetail.set(false);
      }
    });
  }

  closeDetail() {
    this.selectedMember.set(null);
    this.expandedOrderId.set(null);
  }

  toggleOrder(orderId: number) {
    this.expandedOrderId.update(id => id === orderId ? null : orderId);
  }
}