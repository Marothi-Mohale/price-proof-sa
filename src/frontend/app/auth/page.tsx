import { AuthScreen } from "@/components/auth/auth-screen";

export default function AuthPage({ searchParams }: { searchParams?: { next?: string } }) {
  return <AuthScreen nextDestination={searchParams?.next} />;
}
