import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../core/auth.service';
import { ToastService } from '../core/toast.service';

@Component({
  selector: 'app-auth-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './auth-page.component.html',
  styleUrl: './auth-page.component.css'
})
export class AuthPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly toastService = inject(ToastService);

  protected readonly isRegisterMode = signal(false);
  protected readonly isSubmitting = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    name: [''],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    role: ['User']
  });

  constructor() {
    if (this.authService.isLoggedIn()) {
      this.navigateByRole(this.authService.role());
    }
  }

  protected toggleMode(isRegister: boolean): void {
    this.isRegisterMode.set(isRegister);
  }

  protected async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);

    try {
      const value = this.form.getRawValue();
      const session = this.isRegisterMode()
        ? await this.authService.register({
            name: value.name,
            email: value.email,
            password: value.password,
            role: value.role
          })
        : await this.authService.login({
            email: value.email,
            password: value.password
          });

      this.toastService.show(this.isRegisterMode() ? 'Account created successfully.' : 'Welcome back.', 'success');
      this.navigateByRole(session.role);
    } catch (error) {
      console.error(error);
      this.toastService.show('Authentication failed. Please check your details.', 'error');
    } finally {
      this.isSubmitting.set(false);
    }
  }

  private navigateByRole(role: string | null): void {
    this.router.navigateByUrl(role === 'Admin' ? '/admin' : '/helpdesk');
  }
}
