import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiMetricService, DashboardDto } from 'src/app/proxy/metrics'; 
import { ApiMetricDto } from 'src/app/proxy/metrics';
import { PermissionService } from '@abp/ng.core';
import { RouterModule } from '@angular/router';
import jsPDF from 'jspdf';
import autoTable from 'jspdf-autotable'; 

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
  hasAccess = false;

  totalCalls = 0;
  successRate = 0;
  avgTime = 0;

  topSearches: Record<string, number> = {};
  topSearchesKeys: string[] = [];

  ngOnInit() {
    this.hasAccess = this.permissionService.getGrantedPolicy('AbpIdentity.Users');

    if (this.hasAccess) {
      this.loadMetrics();
    } else {
      this.isLoading = false;
    }
  }

  loadMetrics() {
    this.isLoading = true;
    this.metricService.getList({ maxResultCount: 100 } as any).subscribe({
      next: (res) => {
        this.metrics = res.items;
        this.calculateStats();
      },
      error: (err) => {
        console.error(err);
        this.isLoading = false;
      }
    });

    this.metricService.getDashboardStats().subscribe({
      next: (stats: DashboardDto) => {
        this.totalCalls = stats.totalApiCalls;
        this.successRate = stats.successRate;
        this.avgTime = stats.avgResponseTime;
        this.topSearches = stats.topSearches || {};
        this.topSearchesKeys = Object.keys(this.topSearches);

        this.isLoading = false;
      },
      error: (err) => {
        console.error('Error cargando stats de negocio:', err);
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
  downloadCSV() {
   
    const header = ['Fecha', 'Servicio', 'Endpoint', 'Estado', 'Tiempo (ms)'];
    const rows = this.metrics.map(m => [
      new Date(m.creationTime).toLocaleString(),
      m.serviceName,
      m.endpoint,
      m.isSuccess ? 'EXITOSO' : 'FALLIDO',
      m.responseTimeMs
    ]);

    const csvContent =
      header.join(',') + '\n' +
      rows.map(row => row.join(',')).join('\n');

    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.setAttribute('href', url);
    link.setAttribute('download', 'reporte_metricas.csv');
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  }

  downloadPDF() {
    const doc = new jsPDF();

    doc.setFontSize(18);
    doc.text('Reporte de APIs Externas - Pibes del Destino', 14, 20);
    doc.setFontSize(10);
    doc.text(`Generado el: ${new Date().toLocaleString()}`, 14, 30);

    const head = [['Fecha', 'Servicio', 'Endpoint', 'Estado', 'Tiempo']];
    const data = this.metrics.map(m => [
      new Date(m.creationTime).toLocaleString(),
      m.serviceName,
      m.endpoint,
      m.isSuccess ? 'EXITOSO' : 'FALLIDO',
      m.responseTimeMs + ' ms'
    ]);

    autoTable(doc, {
      head: head,
      body: data,
      startY: 35, 
      theme: 'grid', 
      headStyles: { fillColor: [63, 81, 181] } 
    });

    doc.save('reporte_metricas.pdf');
  }
}