import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DestinationService } from '../proxy/destinations';
import { DestinationDto } from '../proxy/application/contracts/destinations';
@Component({
  selector: 'app-popular-destinations',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './popular-destinations.html',
  styleUrls: ['./popular-destinations.scss']
})
export class PopularDestinationsComponent implements OnInit {

  destinations: DestinationDto[] = [];

  constructor(private destinationService: DestinationService) {}

  ngOnInit(): void {
    // Llamamos al GetList del Backend (trae paginado, pedimos los primeros 10)
    this.destinationService.getList({ maxResultCount: 10 }).subscribe(response => {
      this.destinations = response.items || [];
    });
  }
}