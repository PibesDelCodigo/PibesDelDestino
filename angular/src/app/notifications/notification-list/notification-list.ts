import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { NotificationService } from 'src/app/proxy/notifications';
import { AppNotificationDto } from 'src/app/proxy/notifications';
import { ToasterService } from '@abp/ng.theme.shared';
import { RestService } from '@abp/ng.core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-notification-list',
  standalone: true,
  imports: [CommonModule, RouterModule], 
  templateUrl: './notification-list.html',
  styleUrls: ['./notification-list.scss']
})
export class NotificationListComponent implements OnInit {

  private rest = inject(RestService);
  private toaster = inject(ToasterService);
  private router = inject(Router);

  notifications: AppNotificationDto[] = [];
  isLoading = true;
  unreadCount = 0;

  constructor(private notificationService: NotificationService) {}

  ngOnInit(): void {
    this.loadNotifications();
  }

  loadNotifications() {
    this.isLoading = true;
    this.notificationService.getMyNotifications().subscribe({
      next: (list) => {
        this.notifications = list;
        this.unreadCount = list.filter(n => !n.isRead).length;
        this.isLoading = false;
      },
      error: (err) => {
        console.error(err);
        this.isLoading = false;
      }
    });
  }

  markAsRead(item: AppNotificationDto) {
    if (item.isRead) return; 

    this.notificationService.markAsRead(item.id).subscribe(() => {
      item.isRead = true; 
      this.unreadCount--;
    });
  }

  getIcon(type: string): string {
    if (type === 'Comment') return 'fa-comments';
    if (type === 'DestinationUpdate') return 'fa-map-marked-alt';
    return 'fa-bell'; 
  }
  
  getColor(type: string): string {
    if (type === 'Comment') return 'text-primary';       
    if (type === 'DestinationUpdate') return 'text-warning'; 
    return 'text-secondary';
  }

  openSettings() {
    this.router.navigate(['/settings']);
  }

  markAllAsRead() {
    if (this.unreadCount === 0) return; 

    this.isLoading = true;

    this.rest.request({
      method: 'POST',
      url: '/api/app/notification/mark-all-as-read'
    }).subscribe({
      next: () => {
        this.toaster.success('¡Todo limpio! 🧹');
        this.loadNotifications(); 
      },
      error: () => {
        this.toaster.error('No se pudieron marcar como leídas');
        this.isLoading = false;
      }
    });
  }
} 