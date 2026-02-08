import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiMetricService, DashboardDto } from 'src/app/proxy/metrics'; // Asegúrate que DashboardDto esté importado
import { ApiMetricDto } from 'src/app/proxy/metrics';
import { PermissionService } from '@abp/ng.core';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-api-metrics',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './api-metrics.html', // Asegúrate que coincida con tu archivo HTML
  styleUrls: ['./api-metrics.scss']
})
export class ApiMetricsComponent implements OnInit {

  private metricService = inject(ApiMetricService);
  private permissionService = inject(PermissionService);

  metrics: ApiMetricDto[] = [];
  isLoading = true;
  hasAccess = false;

  // Estadísticas Generales
  totalCalls = 0;
  successRate = 0;
  avgTime = 0;

  // 👇 LO NUEVO: Variables para el Top 5 de Búsquedas
  topSearches: Record<string, number> = {};
  topSearchesKeys: string[] = [];

  ngOnInit() {
    // 1. Chequeamos si es Admin
    this.hasAccess = this.permissionService.getGrantedPolicy('AbpIdentity.Users');

    if (this.hasAccess) {
      this.loadMetrics();
    } else {
      this.isLoading = false;
    }
  }

  loadMetrics() {
    this.isLoading = true;

    // A. Pedimos la lista para la Tabla (Tu lógica original)
    this.metricService.getList({ maxResultCount: 100 } as any).subscribe({
      next: (res) => {
        this.metrics = res.items;
        // Calculamos stats locales por si falla el endpoint de dashboard
        this.calculateStats();

        // No ponemos isLoading = false aquí todavía, esperamos al dashboard
      },
      error: (err) => {
        console.error(err);
        this.isLoading = false;
      }
    });

    // 👇 B. LO NUEVO: Pedimos las Estadísticas Oficiales (Top 5 + Cards)
    // Nota: Si typescript se queja de que getDashboardStats no existe, usa (this.metricService as any).getDashboardStats()
    this.metricService.getDashboardStats().subscribe({
      next: (stats: DashboardDto) => {
        // 1. Sobreescribimos los contadores con la data real del servidor
        this.totalCalls = stats.totalApiCalls;
        this.successRate = stats.successRate;
        this.avgTime = stats.avgResponseTime;

        // 2. Cargamos el Top de Búsquedas
        this.topSearches = stats.topSearches || {};
        this.topSearchesKeys = Object.keys(this.topSearches);

        this.isLoading = false; // Ahora sí terminamos
      },
      error: (err) => {
        console.error('Error cargando stats de negocio:', err);
        this.isLoading = false;
      }
    });
  }

  calculateStats() {
    if (this.metrics.length === 0) return;

    // Esta lógica queda como respaldo (fallback)
    this.totalCalls = this.metrics.length;
    const successful = this.metrics.filter(m => m.isSuccess).length;
    this.successRate = (successful / this.totalCalls) * 100;
    const totalTime = this.metrics.reduce((acc, curr) => acc + curr.responseTimeMs, 0);
    this.avgTime = totalTime / this.totalCalls;
  }
}