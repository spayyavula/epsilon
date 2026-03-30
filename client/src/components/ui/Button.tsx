import { forwardRef, type ButtonHTMLAttributes } from 'react';
import { clsx } from 'clsx';

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary' | 'ghost' | 'danger';
  size?: 'sm' | 'md' | 'lg';
}

export const Button = forwardRef<HTMLButtonElement, ButtonProps>(
  ({ className, variant = 'primary', size = 'md', ...props }, ref) => {
    return (
      <button
        ref={ref}
        className={clsx(
          'inline-flex items-center justify-center rounded-xl font-medium',
          'focus:outline-none focus-visible:ring-2 focus-visible:ring-accent/50 focus-visible:ring-offset-2 focus-visible:ring-offset-bg-primary',
          'disabled:opacity-40 disabled:cursor-not-allowed disabled:pointer-events-none',
          'active:scale-[0.97]',
          {
            'bg-accent text-white hover:bg-accent-hover shadow-lg shadow-accent/20 hover:shadow-accent/30': variant === 'primary',
            'bg-bg-tertiary/60 text-text-primary hover:bg-bg-tertiary border border-border hover:border-border-hover': variant === 'secondary',
            'text-text-secondary hover:text-text-primary hover:bg-black/5': variant === 'ghost',
            'bg-danger/10 text-danger hover:bg-danger/20 border border-danger/20': variant === 'danger',
          },
          {
            'px-2.5 py-1.5 text-xs gap-1.5 rounded-lg': size === 'sm',
            'px-4 py-2.5 text-sm gap-2': size === 'md',
            'px-6 py-3 text-base gap-2.5': size === 'lg',
          },
          className
        )}
        {...props}
      />
    );
  }
);
Button.displayName = 'Button';
