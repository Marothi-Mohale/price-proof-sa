import type { InputHTMLAttributes, ReactNode, SelectHTMLAttributes, TextareaHTMLAttributes } from "react";
import { cn } from "@/lib/utils";

type FieldShellProps = {
  htmlFor: string;
  label: string;
  error?: string;
  hint?: string;
  required?: boolean;
  children: ReactNode;
};

export function FieldShell({ htmlFor, label, error, hint, required, children }: FieldShellProps) {
  return (
    <div className="space-y-2">
      <label htmlFor={htmlFor} className="block text-sm font-semibold text-slate-800">
        {label}
        {required ? <span className="ml-1 text-rose-500">*</span> : null}
      </label>
      {children}
      {error ? <p className="text-sm text-rose-600">{error}</p> : null}
      {!error && hint ? <p className="text-sm text-slate-500">{hint}</p> : null}
    </div>
  );
}

export function TextInput({ className, ...props }: InputHTMLAttributes<HTMLInputElement>) {
  return (
    <input
      className={cn(
        "min-h-11 w-full rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-950 shadow-sm outline-none transition placeholder:text-slate-400 focus:border-amber-500 focus:ring-4 focus:ring-amber-500/10",
        className
      )}
      {...props}
    />
  );
}

export function SelectInput({ className, children, ...props }: SelectHTMLAttributes<HTMLSelectElement>) {
  return (
    <select
      className={cn(
        "min-h-11 w-full rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-950 shadow-sm outline-none transition focus:border-amber-500 focus:ring-4 focus:ring-amber-500/10",
        className
      )}
      {...props}
    >
      {children}
    </select>
  );
}

export function TextArea({ className, ...props }: TextareaHTMLAttributes<HTMLTextAreaElement>) {
  return (
    <textarea
      className={cn(
        "min-h-32 w-full rounded-3xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-950 shadow-sm outline-none transition placeholder:text-slate-400 focus:border-amber-500 focus:ring-4 focus:ring-amber-500/10",
        className
      )}
      {...props}
    />
  );
}

export function CheckboxField({
  label,
  description,
  checked,
  onChange,
  id
}: {
  id: string;
  label: string;
  description?: string;
  checked: boolean;
  onChange: (checked: boolean) => void;
}) {
  return (
    <label
      htmlFor={id}
      className="flex cursor-pointer items-start gap-3 rounded-3xl border border-slate-200 bg-white px-4 py-3 transition hover:border-slate-300"
    >
      <input
        id={id}
        type="checkbox"
        checked={checked}
        onChange={(event) => onChange(event.target.checked)}
        className="mt-1 h-4 w-4 rounded border-slate-300 text-amber-600 focus:ring-amber-500"
      />
      <span className="space-y-1">
        <span className="block text-sm font-semibold text-slate-900">{label}</span>
        {description ? <span className="block text-sm text-slate-600">{description}</span> : null}
      </span>
    </label>
  );
}
