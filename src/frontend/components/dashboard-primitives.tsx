import clsx from "clsx";

export function InputField({
  label,
  value,
  onChange,
  type = "text"
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  type?: "text" | "number";
}) {
  return (
    <div>
      <label className="label">{label}</label>
      <input className="input" type={type} value={value} onChange={(event) => onChange(event.target.value)} />
    </div>
  );
}

export function TextAreaField({
  label,
  value,
  onChange
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
}) {
  return (
    <div>
      <label className="label">{label}</label>
      <textarea className="textarea" value={value} onChange={(event) => onChange(event.target.value)} />
    </div>
  );
}

export function SelectField({
  label,
  value,
  onChange,
  options
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  options: ReadonlyArray<{ value: string; label: string }>;
}) {
  return (
    <div>
      <label className="label">{label}</label>
      <select className="input" value={value} onChange={(event) => onChange(event.target.value)}>
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
    </div>
  );
}

export function ToggleField({
  label,
  checked,
  onChange
}: {
  label: string;
  checked: boolean;
  onChange: (checked: boolean) => void;
}) {
  return (
    <label className="flex items-center justify-between rounded-2xl border border-slate-200 bg-white px-4 py-3">
      <span className="text-sm font-semibold text-slate-700">{label}</span>
      <input checked={checked} onChange={(event) => onChange(event.target.checked)} type="checkbox" />
    </label>
  );
}

export function FileField({
  label,
  accept,
  onChange
}: {
  label: string;
  accept: string;
  onChange: (file: File | null) => void;
}) {
  return (
    <div>
      <label className="label">{label}</label>
      <input
        className="input file:mr-3 file:rounded-full file:border-0 file:bg-ink file:px-4 file:py-2 file:text-sm file:font-semibold file:text-white"
        accept={accept}
        onChange={(event) => onChange(event.target.files?.[0] ?? null)}
        type="file"
      />
    </div>
  );
}

export function StatusBanner({ tone, message }: { tone: "success" | "error"; message: string }) {
  return (
    <div
      className={clsx(
        "panel p-4 text-sm font-medium",
        tone === "success" ? "border-emerald-200 bg-emerald-50 text-emerald-900" : "border-red-200 bg-red-50 text-red-900"
      )}
    >
      {message}
    </div>
  );
}

export function EmptyState({
  title,
  body,
  compact = false
}: {
  title: string;
  body: string;
  compact?: boolean;
}) {
  return (
    <div className={clsx("rounded-3xl border border-dashed border-slate-300 bg-white/70 text-center", compact ? "p-4" : "p-8")}>
      <h4 className="text-lg font-semibold">{title}</h4>
      <p className="mt-2 text-sm leading-6 text-slate-500">{body}</p>
    </div>
  );
}

export function MetricChip({
  label,
  value,
  tone = "neutral"
}: {
  label: string;
  value: string;
  tone?: "neutral" | "warn" | "good";
}) {
  return (
    <div
      className={clsx(
        "rounded-2xl border px-4 py-3",
        tone === "warn" && "border-amber-200 bg-amber-50 text-amber-900",
        tone === "good" && "border-emerald-200 bg-emerald-50 text-emerald-900",
        tone === "neutral" && "border-slate-200 bg-white text-slate-800"
      )}
    >
      <p className="text-xs font-semibold uppercase tracking-[0.16em]">{label}</p>
      <p className="mt-1 text-lg font-bold">{value}</p>
    </div>
  );
}

export function StatusPill({ label, flagged }: { label: string; flagged: boolean }) {
  return (
    <span className={clsx("rounded-full px-3 py-1 text-xs font-semibold", flagged ? "bg-amber-100 text-amber-900" : "bg-slate-100 text-slate-700")}>
      {label}
    </span>
  );
}

export function TimelineCard({
  title,
  subtitle,
  body,
  meta
}: {
  title: string;
  subtitle: string;
  body: string;
  meta: string;
}) {
  return (
    <div className="rounded-3xl border border-slate-200 bg-white p-4">
      <p className="font-semibold text-slate-900">{title}</p>
      <p className="mt-1 text-sm text-slate-500">{subtitle}</p>
      <p className="mt-3 text-sm leading-6 text-slate-700">{body}</p>
      <p className="mt-2 text-xs font-medium uppercase tracking-[0.16em] text-slate-400">{meta}</p>
    </div>
  );
}

export function InfoTile({ title, body }: { title: string; body: string }) {
  return (
    <div className="rounded-3xl border border-slate-200 bg-white/70 p-5">
      <h3 className="text-lg font-semibold">{title}</h3>
      <p className="mt-2 text-sm leading-6 text-slate-600">{body}</p>
    </div>
  );
}
