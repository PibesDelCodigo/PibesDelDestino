import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NotificationService } from 'src/app/proxy/notifications';
import { AppNotificationDto } from 'src/app/proxy/notifications';

@Component({
  selector: 'app-notification-list',
  standalone: true,
  imports: [CommonModule],
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
    if (item.isRead) return; // Si ya está leída, no hacemos nada

    this.notificationService.markAsRead(item.id).subscribe(() => {
      item.isRead = true; // Actualizamos visualmente al instante
    });
  }

  // Helper para elegir el ícono según el tipo
  getIcon(type: string): string {
    if (type === 'Comment') return 'fa-comments';
    if (type === 'DestinationUpdate') return 'fa-map-marked-alt';
    return 'fa-bell'; // Default
  }
  
  // Helper para el color del ícono
  getColor(type: string): string {
    if (type === 'Comment') return 'text-primary';       // Azul
    if (type === 'DestinationUpdate') return 'text-warning'; // Amarillo/Naranja
    return 'text-secondary';
  }
}