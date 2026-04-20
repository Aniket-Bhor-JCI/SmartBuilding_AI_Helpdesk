import { CommonModule, DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../core/auth.service';
import { ChatService } from '../core/chat.service';
import { ChatAnalysis, MessageBubble, Ticket } from '../core/models';
import { TicketService } from '../core/ticket.service';
import { ToastService } from '../core/toast.service';

@Component({
  selector: 'app-helpdesk-page',
  standalone: true,
  imports: [CommonModule, FormsModule, DatePipe],
  templateUrl: './helpdesk-page.component.html',
  styleUrl: './helpdesk-page.component.css'
})
export class HelpdeskPageComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly chatService = inject(ChatService);
  private readonly ticketService = inject(TicketService);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);

  protected readonly session = this.authService.session;
  protected readonly messages = signal<MessageBubble[]>([
    { sender: 'bot', text: 'Hello. Describe your building issue and I will suggest a quick fix first.' }
  ]);
  protected readonly tickets = signal<Ticket[]>([]);
  protected readonly draft = signal('');
  protected readonly isSending = signal(false);
  protected readonly pendingAnalysis = signal<ChatAnalysis | null>(null);
  protected readonly typing = signal(false);
  protected readonly hasTickets = computed(() => this.tickets().length > 0);

  async ngOnInit(): Promise<void> {
    await this.loadTickets();
  }

  protected async sendMessage(): Promise<void> {
    const message = this.draft().trim();
    if (!message || this.isSending()) {
      return;
    }

    this.messages.update((items) => [...items, { sender: 'user', text: message }]);
    this.draft.set('');
    this.isSending.set(true);
    this.typing.set(true);
    this.pendingAnalysis.set(null);

    try {
      const analysis = await this.chatService.analyze(message);
      this.messages.update((items) => [...items, { sender: 'bot', text: analysis.botMessage }]);
      this.pendingAnalysis.set(analysis.shouldOfferTicket ? analysis : null);

      if (analysis.requiresHumanHandoff) {
        this.toastService.show('Human follow-up recommended for this issue.', 'info');
      }
    } catch (error) {
      console.error(error);
      this.messages.update((items) => [...items, { sender: 'bot', text: 'I could not process that message right now. Please try again.' }]);
      this.toastService.show('Chat request failed.', 'error');
    } finally {
      this.isSending.set(false);
      this.typing.set(false);
    }
  }

  protected async confirmTicketCreation(confirmed: boolean): Promise<void> {
    const analysis = this.pendingAnalysis();
    if (!analysis) {
      return;
    }

    if (!confirmed) {
      this.messages.update((items) => [...items, { sender: 'bot', text: 'No problem. I will keep this as a suggestion only.' }]);
      this.pendingAnalysis.set(null);
      return;
    }

    try {
      await this.ticketService.createTicket({
        issue: analysis.issue,
        category: analysis.category,
        location: analysis.location,
        priority: analysis.priority
      });
      this.messages.update((items) => [...items, { sender: 'bot', text: 'Your ticket has been created successfully.' }]);
      this.toastService.show('Ticket created.', 'success');
      this.pendingAnalysis.set(null);
      await this.loadTickets();
    } catch (error) {
      console.error(error);
      this.toastService.show('Ticket creation failed.', 'error');
    }
  }

  protected logout(): void {
    this.authService.logout();
  }

  protected goToAdmin(): void {
    this.router.navigateByUrl('/admin');
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
