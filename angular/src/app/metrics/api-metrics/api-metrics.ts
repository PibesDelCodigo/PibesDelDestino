import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiMetricService} from 'src/app/proxy/metrics';
import { ApiMetricDto } from 'src/app/proxy/metrics';
import { PermissionService } from '@abp/ng.core';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-api-metrics',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './api-metrics.html',
  styleUrls: ['./api-metrics.scss']
})
export class ApiMetricsComponent implements OnInit {
  
  private metricService = inject(ApiMetricService);
  private permissionService = inject(PermissionService);

  metrics: ApiMetricDto[] = [];
  isLoading = true;
  hasAccess = false; // 👈 Bandera para saber si mostramos o no
  
  // Estadísticas
  totalCalls = 0;
  successRate = 0;
  avgTime = 0;

  ngOnInit() {
    // 1. Chequeamos si es Admin (usando el permiso de usuarios como referencia)
    this.hasAccess = this.permissionService.getGrantedPolicy('AbpIdentity.Users');

    if (this.hasAccess) {
      this.loadMetrics();
    } else {
      this.isLoading = false; // Dejamos de cargar pero no pedimos datos
    }
  }

  loadMetrics() {
    this.isLoading = true;

    // Ahora sí pedimos datos (el backend ya no dará error por el ServiceName opcional)
    this.metricService.getList({ maxResultCount: 100 } as any).subscribe({
      next: (res) => {
        this.metrics = res.items;
        this.calculateStats();
        this.isLoading = false;
      },
      error: (err) => {
        console.error(err);
        this.isLoading = false;
      }
    });
  }

  calculateStats() {
    if (this.metrics.length === 0) return;

    this.totalCalls = this.metrics.length;
    const successful = this.metrics.filter(m => m.isSuccess).length;
    this.successRate = (successful / this.totalCalls) * 100;
    const totalTime = this.metrics.reduce((acc, curr) => acc + curr.responseTimeMs, 0);
    this.avgTime = totalTime / this.totalCalls;
  }
}