import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router'; // 👈 1. Importamos esto
import { NotificationService } from 'src/app/proxy/notifications';
import { AppNotificationDto } from 'src/app/proxy/notifications';

@Component({
  selector: 'app-notification-list',
  standalone: true,
  imports: [CommonModule, RouterModule], // 👈 2. Lo agregamos acá
  templateUrl: './notification-list.html',
  styleUrls: ['./notification-list.scss']
})
export class NotificationListComponent implements OnInit {

  notifications: AppNotificationDto[] = [];
  isLoading = true;

  constructor(private notificationService: NotificationService) {}

  ngOnInit(): void {
    this.loadNotifications();
  }

  loadNotifications() {
    this.isLoading = true;
    this.notificationService.getMyNotifications().subscribe({
      next: (list) => {
        this.notifications = list;
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
}