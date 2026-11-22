export type Row = Record<string, any>;
let token = "";
async function csrf() {
  const r = await fetch("/api/auth/csrf");
  token = (await r.json()).token;
}
export async function api(
  path: string,
  method = "GET",
  body?: unknown,
): Promise<any> {
  if (method !== "GET" && !token) await csrf();
  const form = body instanceof FormData;
  const r = await fetch("/api/" + path, {
    method,
    credentials: "same-origin",
    headers: {
      ...(method !== "GET" ? { "X-CSRF-TOKEN": token } : {}),
      ...(!form && body ? { "Content-Type": "application/json" } : {}),
    },
    body: form ? body : body ? JSON.stringify(body) : undefined,
  });
  if (!r.ok) {
    if (r.status === 401 && path !== "auth/login")
      window.dispatchEvent(new Event("session-expired"));
    let msg = "Unable to complete the operation.";
    try {
      const e = await r.json();
      msg =
        e.error ||
        Object.values(e.errors || {})
          .flat()
          .join(" ") ||
        msg;
    } catch {}
    throw new Error(r.status === 401 ? "Please sign in to continue." : msg);
  }
  if (method !== "GET" && path.startsWith("auth/")) token = "";
  const t = await r.text();
  return t ? JSON.parse(t) : null;
}
export const kes = (n: unknown) =>
  "KES " + Number(n || 0).toLocaleString("en-KE", { maximumFractionDigits: 2 });
export const date = (s: string) =>
  new Date(s).toLocaleDateString("en-GB", {
    day: "2-digit",
    month: "short",
    year: "numeric",
  });
