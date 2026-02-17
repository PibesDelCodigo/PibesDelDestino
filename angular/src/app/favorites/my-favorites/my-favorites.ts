import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router'; 
import { FavoriteService } from 'src/app/proxy/favorites';
import { DestinationDto } from 'src/app/proxy/application/contracts/destinations';

@Component({
  selector: 'app-my-favorites',
  standalone: true,
  imports: [CommonModule, RouterModule], 
  templateUrl: './my-favorites.html', 
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
    if (!confirm('¿Sacar este destino de tu lista de deseos?')) {
      return;
    }

    const backup = [...this.favorites];
    this.favorites = this.favorites.filter(d => d.id !== id);

    this.favoriteService.toggle({ destinationId: id }).subscribe({
      next: () => {
      },
      error: (err) => {
        this.favorites = backup;
        alert('No se pudo eliminar el favorito. Intente nuevamente.');
      }
    });
  }
}