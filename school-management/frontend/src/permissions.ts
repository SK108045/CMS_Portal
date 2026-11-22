import type { Row } from "./api";
// Effective capabilities come from the API. No role-based UI authorisation rules live here.
export const has = (user: Row | undefined | null, permission: string) =>
  !!user?.permissions?.includes(permission);
export const read = (user: Row | undefined | null, resource: string) =>
  !!user?.access?.[resource]?.read;
export const create = (user: Row | undefined | null, resource: string) =>
  !!user?.access?.[resource]?.create;
export const edit = (user: Row | undefined | null, resource: string) =>
  !!user?.access?.[resource]?.edit;
export const all = (user: Row | undefined | null, resource: string) =>
  !!user?.access?.[resource]?.all;
export const management = (user: Row | undefined | null) =>
  has(user, "reports.view.all") || has(user, "users.manage");
export const roleLabel = (role: string) =>
  role
    .toLowerCase()
    .split("_")
    .map((w) => w[0].toUpperCase() + w.slice(1))
    .join(" ");
