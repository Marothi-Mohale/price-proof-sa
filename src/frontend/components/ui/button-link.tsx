import Link, { type LinkProps } from "next/link";
import type { AnchorHTMLAttributes, ReactNode } from "react";
import { buttonClasses } from "@/components/ui/button";

export function ButtonLink({
  href,
  children,
  variant = "primary",
  className,
  ...props
}: LinkProps &
  Omit<AnchorHTMLAttributes<HTMLAnchorElement>, "href"> & {
    children: ReactNode;
    variant?: "primary" | "secondary" | "ghost" | "danger";
  }) {
  return (
    <Link href={href} className={buttonClasses(variant, className)} {...props}>
      {children}
    </Link>
  );
}
