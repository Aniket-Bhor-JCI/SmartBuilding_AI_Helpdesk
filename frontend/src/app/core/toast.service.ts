import { Injectable, signal } from '@angular/core';

export interface ToastItem {
  id: number;
  message: string;
  type: 'success' | 'error' | 'info';
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  readonly toasts = signal<ToastItem[]>([]);

  show(message: string, type: ToastItem['type'] = 'info'): void {
    const item = { id: Date.now() + Math.random(), message, type };
    this.toasts.update((current) => [...current, item]);
    window.setTimeout(() => this.dismiss(item.id), 3000);
  }

  dismiss(id: number): void {
    this.toasts.update((current) => current.filter((toast) => toast.id !== id));
  }
}
