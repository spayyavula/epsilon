import { forwardRef, type TextareaHTMLAttributes } from 'react';
import { clsx } from 'clsx';

export const Textarea = forwardRef<HTMLTextAreaElement, TextareaHTMLAttributes<HTMLTextAreaElement>>(
  ({ className, ...props }, ref) => {
    return (
      <textarea
        ref={ref}
        className={clsx(
          'w-full bg-bg-primary/50 border border-border rounded-xl px-3.5 py-2.5 text-sm text-text-primary',
          'placeholder:text-text-muted/60',
          'focus:outline-none focus:ring-2 focus:ring-accent/30 focus:border-accent/50',
          'hover:border-border-hover resize-none',
          className
        )}
        {...props}
      />
    );
  }
);
Textarea.displayName = 'Textarea';
