import { Component, OnInit } from '@angular/core';
import { NavItemsService, eThemeSharedRouteNames, LoaderBarComponent } from '@abp/ng.theme.shared';
import { NotificationBellComponent } from './notifications/notification-bell/notification-bell';
import { DynamicLayoutComponent } from "@abp/ng.core";

@Component({
  selector: 'app-root',
  template: `
    <abp-loader-bar></abp-loader-bar>
    <abp-dynamic-layout></abp-dynamic-layout>
  `,
  imports: [LoaderBarComponent, DynamicLayoutComponent],
})
export class AppComponent implements OnInit {

  constructor(private navItems: NavItemsService) {}

  ngOnInit(): void {
    this.navItems.addItems([
      {
        id: 'NotificationBell',
        order: 1, 
        component: NotificationBellComponent, 
      },
    ]);
  }
}