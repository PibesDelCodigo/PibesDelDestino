import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router'; // Para leer la URL
import { AppUserService } from 'src/app/proxy/users';
import { PublicUserDto } from 'src/app/proxy/users';

@Component({
  selector: 'app-public-profile',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './public-profile.html',
  styleUrls: ['./public-profile.scss']
})
export class PublicProfile implements OnInit {
  
  user: PublicUserDto | null = null;
  isLoading = true;
  errorMessage = '';

  constructor(
    private route: ActivatedRoute,
    private userService: AppUserService
  ) {}

  ngOnInit(): void {
    // 1. Leemos el ID de la URL
    const userId = this.route.snapshot.paramMap.get('id');

    if (userId) {
      this.loadProfile(userId);
    } else {
      this.errorMessage = 'ID de usuario no válido.';
      this.isLoading = false;
    }
  }

  loadProfile(id: string) {
    // 2. Llamamos al backend
    this.userService.getPublicProfile(id).subscribe({
      next: (data) => {
        this.user = data;
        this.isLoading = false;
      },
      error: (err) => {
        console.error(err);
        this.errorMessage = 'No se pudo cargar el perfil. ¿El usuario existe?';
        this.isLoading = false;
      }
    });
  }
}