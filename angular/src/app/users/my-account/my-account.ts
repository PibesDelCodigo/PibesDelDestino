import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AppUserService } from 'src/app/proxy/users';
import { AuthService } from '@abp/ng.core'; // Para cerrar sesión después de borrar
import { Router } from '@angular/router';


@Component({
  selector: 'app-my-account',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './my-account.html',
  styleUrls: ['./my-account.scss']
})
export class MyAccount {


  constructor(
    private userService: AppUserService,
    private authService: AuthService,
    private router: Router
  ) {}


  deleteAccount() {
    if (confirm('¿Estás SEGURO de que querés eliminar tu cuenta? Esta acción no se puede deshacer.')) {
      this.userService.deleteSelf().subscribe({
        next: () => {
          alert('Tu cuenta ha sido eliminada.');
          this.authService.logout().subscribe(() => {
             this.router.navigate(['/']);
          });
        },
        error: (err) => {
          console.error(err);
          alert('Ocurrió un error al intentar eliminar la cuenta.');
        }
      });
    }
  }
}
