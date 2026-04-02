import { NextRequest, NextResponse } from "next/server";

const upstreamOrigins = ["http://127.0.0.1:5250", "http://127.0.0.1:8080", "http://api:8080", "http://backend:8080"];
const passthroughHeaderNames = [
  "accept",
  "accept-language",
  "authorization",
  "content-type",
  "cookie",
  "origin",
  "referer",
  "user-agent",
  "x-correlation-id"
] as const;

export const dynamic = "force-dynamic";

async function forwardRequest(request: NextRequest, pathSegments: string[]) {
  const path = pathSegments.join("/");
  const search = request.nextUrl.search;
  const method = request.method.toUpperCase();
  const bodyBuffer =
    method === "GET" || method === "HEAD"
      ? undefined
      : Buffer.from(await request.arrayBuffer());
  const headers = new Headers();

  for (const headerName of passthroughHeaderNames) {
    const headerValue = request.headers.get(headerName);

    if (headerValue) {
      headers.set(headerName, headerValue);
    }
  }

  for (const origin of upstreamOrigins) {
    try {
      const upstreamResponse = await fetch(`${origin}/${path}${search}`, {
        method,
        headers,
        body: bodyBuffer ? Buffer.from(bodyBuffer) : undefined,
        cache: "no-store",
        redirect: "manual"
      });

      const responseHeaders = new Headers(upstreamResponse.headers);
      responseHeaders.delete("content-encoding");
      responseHeaders.delete("transfer-encoding");

      return new NextResponse(upstreamResponse.body, {
        status: upstreamResponse.status,
        headers: responseHeaders
      });
    } catch {
    }
  }

  return NextResponse.json(
    {
      title: "Backend unavailable",
      detail: "PriceProof SA could not reach the backend service."
    },
    { status: 503 }
  );
}

type RouteContext = {
  params: {
    path: string[];
  };
};

export async function GET(request: NextRequest, context: RouteContext) {
  return forwardRequest(request, context.params.path);
}

export async function POST(request: NextRequest, context: RouteContext) {
  return forwardRequest(request, context.params.path);
}

export async function PUT(request: NextRequest, context: RouteContext) {
  return forwardRequest(request, context.params.path);
}

export async function PATCH(request: NextRequest, context: RouteContext) {
  return forwardRequest(request, context.params.path);
}

export async function DELETE(request: NextRequest, context: RouteContext) {
  return forwardRequest(request, context.params.path);
}

export async function OPTIONS(request: NextRequest, context: RouteContext) {
  return forwardRequest(request, context.params.path);
}

export async function HEAD(request: NextRequest, context: RouteContext) {
  return forwardRequest(request, context.params.path);
}
