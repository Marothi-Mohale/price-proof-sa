import { Suspense } from "react";
import { VerifyEmailScreen } from "@/components/auth/verify-email-screen";

export default function VerifyEmailPage() {
  return (
    <Suspense fallback={null}>
      <VerifyEmailScreen />
    </Suspense>
  );
}
