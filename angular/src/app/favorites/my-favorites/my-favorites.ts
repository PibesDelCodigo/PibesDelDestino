import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FavoriteService } from 'src/app/proxy/favorites';
import { DestinationDto } from 'src/app/proxy/application/contracts/destinations';

@Component({
  selector: 'app-my-favorites',
  standalone: true,
  imports: [CommonModule],
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
        console.log('Favoritos cargados:', this.favorites.length);
      },
      error: (err) => {
        console.error('Error cargando favoritos:', err);
        this.isLoading = false;
      }
    });
  }

  // --- FUNCIÓN REMOVE (DEBUGGEADA) ---
  remove(id: string) {
    console.log('1. Clic detectado en ID:', id);

    // 1. Confirmación visual (Para saber si el botón recibe el clic)
    if (!confirm('¿Seguro que querés quitar este destino de favoritos?')) {
      return;
    }

    console.log('2. Enviando petición al backend...');

    // 2. Llamada al servicio
    this.favoriteService.toggle({ destinationId: id }).subscribe({
      next: () => {
        console.log('3. Backend respondió éxito. Actualizando lista...');
        
        // 3. Actualización visual (Filtro)
        const cantidadAntes = this.favorites.length;
        this.favorites = this.favorites.filter(d => d.id !== id);
        const cantidadDespues = this.favorites.length;

        console.log(`Lista actualizada: de ${cantidadAntes} a ${cantidadDespues} elementos.`);
      },
      error: (err) => {
        console.error('4. Error en el Backend:', err);
        alert('❌ Ocurrió un error al intentar borrar.');
      }
    });
  }
}