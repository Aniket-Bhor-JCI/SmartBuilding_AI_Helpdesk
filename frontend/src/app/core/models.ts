export interface AuthResponse {
  token: string;
  name: string;
  email: string;
  role: 'User' | 'Admin';
}

export interface UserSession extends AuthResponse {}

export interface ChatAnalysis {
  issue: string;
  category: string;
  location?: string | null;
  priority: string;
  solution: string;
  intent: string;
  confidence: number;
  requiresHumanHandoff: boolean;
  handoffReason?: string | null;
  shouldOfferTicket: boolean;
  botMessage: string;
}

export interface Ticket {
  id: number;
  issue: string;
  category: string;
  location?: string | null;
  priority: string;
  status: string;
  createdAt: string;
  createdBy: number;
  createdByName?: string | null;
  assignedTo?: string | null;
}

export interface MessageBubble {
  sender: 'user' | 'bot';
  text: string;
}
