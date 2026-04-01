import { clsx, type ClassValue } from "clsx";
import type { EvidenceType } from "@/lib/types";

export function cn(...inputs: ClassValue[]) {
  return clsx(inputs);
}

export function toDateTimeLocalInput(value?: string | Date | null) {
  const source = value instanceof Date ? value : value ? new Date(value) : new Date();

  const year = source.getFullYear();
  const month = String(source.getMonth() + 1).padStart(2, "0");
  const day = String(source.getDate()).padStart(2, "0");
  const hours = String(source.getHours()).padStart(2, "0");
  const minutes = String(source.getMinutes()).padStart(2, "0");

  return `${year}-${month}-${day}T${hours}:${minutes}`;
}

export function toUtcIsoString(value: string) {
  return new Date(value).toISOString();
}

export function formatFileSize(bytes: number) {
  if (bytes < 1024) {
    return `${bytes} B`;
  }

  if (bytes < 1024 * 1024) {
    return `${(bytes / 1024).toFixed(1)} KB`;
  }

  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

export function deriveEvidenceType(file: File | null): EvidenceType {
  if (!file) {
    return "Text";
  }

  const mimeType = file.type.toLowerCase();

  if (mimeType.startsWith("image/")) {
    return "Image";
  }

  if (mimeType === "application/pdf") {
    return "Pdf";
  }

  if (mimeType.startsWith("audio/")) {
    return "Audio";
  }

  if (mimeType.startsWith("video/")) {
    return "Video";
  }

  if (mimeType.includes("json")) {
    return "Json";
  }

  return "Text";
}

export function isVisualPreview(contentType?: string | null) {
  return Boolean(contentType?.startsWith("image/"));
}

export function createTextEvidenceFile(fileName: string, content: string) {
  return new File([content], fileName, {
    type: "text/plain;charset=utf-8"
  });
}
