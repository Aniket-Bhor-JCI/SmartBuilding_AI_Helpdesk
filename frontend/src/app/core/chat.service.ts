import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ChatAnalysis } from './models';

@Injectable({ providedIn: 'root' })
export class ChatService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = '/api/chat';

  analyze(message: string): Promise<ChatAnalysis> {
    return firstValueFrom(this.http.post<ChatAnalysis>(this.apiUrl, { message }));
  }
}
