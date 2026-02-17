import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router'; // Para leer la URL
import { DestinationService } from 'src/app/proxy/destinations';
import { DestinationDto } from 'src/app/proxy/application/contracts/destinations';
import { CommonModule } from '@angular/common';
import { ExperienceListComponent } from 'src/app/experiences/experience-list/experience-list';

@Component({
  selector: 'app-destination-detail',
  standalone: true,
  imports: [CommonModule, ExperienceListComponent],
  templateUrl: './destination-detail.html',
  styleUrls: ['./destination-detail.scss']
})
export class DestinationDetailComponent implements OnInit {
  
  destinationId: string = '';
  destination: DestinationDto | null = null;

  constructor(
    private route: ActivatedRoute,
    private destinationService: DestinationService
  ) {}

  ngOnInit(): void {
    this.destinationId = this.route.snapshot.params['id'];
    if (this.destinationId) {
      this.destinationService.get(this.destinationId).subscribe(res => {
        this.destination = res;
      });
    }
  }
}