import { CommonModule, DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../core/auth.service';
import { Ticket } from '../../core/models';
import { TicketService } from '../../core/ticket.service';
import { ToastService } from '../../core/toast.service';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, DatePipe],
  templateUrl: './admin-dashboard.component.html',
  styleUrl: './admin-dashboard.component.css'
})
export class AdminDashboardComponent implements OnInit {
  private readonly ticketService = inject(TicketService);
  private readonly authService = inject(AuthService);
  private readonly toastService = inject(ToastService);

  protected readonly tickets = signal<Ticket[]>([]);
  protected readonly openCount = computed(() => this.tickets().filter((ticket) => ticket.status === 'Open').length);
  protected readonly inProgressCount = computed(() => this.tickets().filter((ticket) => ticket.status === 'In Progress').length);
  protected readonly resolvedCount = computed(() => this.tickets().filter((ticket) => ticket.status === 'Resolved').length);

  async ngOnInit(): Promise<void> {
    await this.loadTickets();
  }

  protected async saveTicket(ticket: Ticket): Promise<void> {
    try {
      const updated = await this.ticketService.updateTicket(ticket.id, {
        status: ticket.status,
        priority: ticket.priority,
        assignedTo: ticket.assignedTo ?? ''
      });

      this.tickets.update((items) => items.map((item) => item.id === updated.id ? updated : item));
      this.toastService.show(`Ticket #${ticket.id} updated.`, 'success');
    } catch (error) {
      console.error(error);
      this.toastService.show('Update failed.', 'error');
    }
  }

  protected logout(): void {
    this.authService.logout();
  }

  private async loadTickets(): Promise<void> {
    try {
      this.tickets.set(await this.ticketService.getTickets());
    } catch (error) {
      console.error(error);
      this.toastService.show('Could not load tickets.', 'error');
    }
  }
}
