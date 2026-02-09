import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router'; // <--- IMPORTANTE PARA LA NAVEGACIÓN
import { FavoriteService } from 'src/app/proxy/favorites';
import { DestinationDto } from 'src/app/proxy/application/contracts/destinations';

@Component({
  selector: 'app-my-favorites',
  standalone: true,
  imports: [CommonModule, RouterModule], // <--- AGREGADO AQUÍ
  templateUrl: './my-favorites.html', // Asegurate que coincida con tu nombre de archivo
  styleUrls: ['./my-favorites.scss']
})
export class MyFavoritesComponent implements OnInit {

  favorites: DestinationDto[] = [];
  isLoading = true;

  constructor(private favoriteService: FavoriteService) {}

  ngOnInit(): void {
    this.loadFavorites();
  }

  loadFavorites() {
    this.favoriteService.getMyFavorites().subscribe({
      next: (data) => {
        this.favorites = data;
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Error cargando favoritos:', err);
        this.isLoading = false;
      }
    });
  }

  remove(id: string) {
    // Confirmación simple nativa (podríamos hacerla más linda después, pero cumple)
    if (!confirm('¿Sacar este destino de tu lista de deseos?')) {
      return;
    }

    // Actualización optimista: Lo borramos de la vista inmediatamente para que se sienta rápido
    const backup = [...this.favorites];
    this.favorites = this.favorites.filter(d => d.id !== id);

    this.favoriteService.toggle({ destinationId: id }).subscribe({
      next: () => {
        // Todo salió bien, no hacemos nada más
      },
      error: (err) => {
        // Si falló, restauramos la lista y avisamos
        this.favorites = backup;
        alert('No se pudo eliminar el favorito. Intente nuevamente.');
      }
    });
  }
}