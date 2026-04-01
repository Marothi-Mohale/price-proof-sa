import { ProtectedAppShell } from "@/components/layout/protected-app-shell";

export default function PlatformLayout({ children }: { children: React.ReactNode }) {
  return <ProtectedAppShell>{children}</ProtectedAppShell>;
}
