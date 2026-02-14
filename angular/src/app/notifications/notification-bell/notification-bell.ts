import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core'; // <--- 1. IMPORTAR
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { NotificationService } from 'src/app/proxy/notifications';
import { AuthService, ConfigStateService } from '@abp/ng.core';

@Component({
  selector: 'app-notification-bell',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './notification-bell.html',
  styleUrls: ['./notification-bell.scss']
})
export class NotificationBellComponent implements OnInit, OnDestroy {

  unreadCount = 0;
  private intervalId: any;

  constructor(
    private notificationService: NotificationService,
    private router: Router,
    private authService: AuthService,
    private config: ConfigStateService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.config.getOne$('currentUser').subscribe(currentUser => {
      if (currentUser) {
        this.refreshCount();
      }
    });

    this.intervalId = setInterval(() => {
      this.refreshCount();
    }, 15000);
  }

  ngOnDestroy(): void {
    if (this.intervalId) {
      clearInterval(this.intervalId);
    }
  }

  refreshCount() {
    if (this.authService.isAuthenticated) {
      this.notificationService.getUnreadCount().subscribe({
        next: (count) => {
          this.unreadCount = count;
          this.cdr.detectChanges(); 
        },
        error: () => {}
      });
    }
  }

  goToNotifications() {
    this.router.navigate(['/notifications']);
  }
}