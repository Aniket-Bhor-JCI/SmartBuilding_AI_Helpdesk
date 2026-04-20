import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { ToastService } from '../core/toast.service';

@Component({
  selector: 'app-toast',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="toast-stack">
      @for (toast of toastService.toasts(); track toast.id) {
        <div class="toast" [class.success]="toast.type === 'success'" [class.error]="toast.type === 'error'">
          <span>{{ toast.message }}</span>
          <button type="button" (click)="toastService.dismiss(toast.id)">x</button>
        </div>
      }
    </div>
  `,
  styles: [`
    .toast-stack {
      position: fixed;
      top: 20px;
      right: 20px;
      display: grid;
      gap: 12px;
      z-index: 20;
    }

    .toast {
      min-width: 260px;
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 12px;
      padding: 14px 16px;
      border-radius: 16px;
      border: 1px solid var(--border);
      background: var(--surface);
      box-shadow: var(--shadow);
    }

    .toast.success {
      border-color: rgba(31, 143, 95, 0.25);
    }

    .toast.error {
      border-color: rgba(214, 69, 69, 0.25);
    }

    button {
      border: none;
      background: transparent;
      color: var(--muted);
    }
  `]
})
export class ToastComponent {
  protected readonly toastService = inject(ToastService);
}
