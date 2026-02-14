import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root' 
})
export class CitySearchStateService {
    lastFormValues: any = {}; 
    lastResults: any[] = [];
    lastCache = new Map<string, { id: string, rating: number }>();

  hasData(): boolean {
    return this.lastResults.length > 0;
  }
}