import { test, expect, Page } from "@playwright/test";
const password = "SchoolDemo123!";
async function login(page: Page, email: string, pw = password) {
  await page.goto("/");
  await page.getByLabel("Email", { exact: true }).fill(email);
  await page.getByLabel("Password", { exact: true }).fill(pw);
  await page.getByRole("button", { name: "Sign in →" }).click();
  await expect(
    page.getByRole("heading", {
      name: /Every shilling, accounted for\.|Approve purchase requests/,
    }),
  ).toBeVisible();
  return (await page.request.get("/api/auth/me")).json();
}
async function denied(page: Page, path: string, body: object = {}) {
  const token = (await (await page.request.get("/api/auth/csrf")).json()).token;
  const response = await page.request.post("/api/" + path, {
    headers: { "X-CSRF-TOKEN": token },
    data: body,
  });
  expect(response.status()).toBe(403);
}
test("Finance has financial actions and cannot access hidden system administration", async ({
  page,
}) => {
  const me = await login(page, "finance@missionschool.local");
  expect(me.permissions).toContain("payments.create");
  expect(me.permissions).not.toContain("users.manage");
  await expect(
    page.getByRole("link", { name: "Users", exact: true }),
  ).toHaveCount(0);
  await page.goto("/#payments");
  await expect(
    page.getByRole("button", { name: "+ New record" }),
  ).toBeVisible();
  await page.goto("/#budgets");
  await expect(page.getByRole("button", { name: "+ New record" })).toHaveCount(
    0,
  );
  await page.goto("/#departments");
  await expect(page.getByRole("button", { name: "+ New record" })).toHaveCount(
    0,
  );
  await denied(page, "records/users");
  await denied(page, "records/departments");
});
test("Storekeeper has receiving controls without financial amounts or payment actions", async ({
  page,
}) => {
  await login(page, "storekeeper@missionschool.local");
  for (const name of [
    "Users",
    "Budgets",
    "Payments",
    "Expenses",
    "Income & Donations",
  ])
    await expect(page.getByRole("link", { name, exact: true })).toHaveCount(0);
  await page.goto("/#inventory");
  await expect(
    page.getByRole("button", { name: "+ New record" }),
  ).toBeVisible();
  await page.goto("/#requests");
  await expect(
    page.getByRole("columnheader", { name: /Approved Amount/ }),
  ).toHaveCount(0);
  await expect(page.getByRole("button", { name: "+ New request" })).toHaveCount(
    0,
  );
  await page
    .locator(".table-panel")
    .getByRole("button", { name: "Open ↗" })
    .first()
    .click();
  await expect(
    page.getByText("Approved amount (KES)", { exact: true }),
  ).toHaveCount(0);
  await expect(
    page.getByRole("button", { name: "Record payment" }),
  ).toHaveCount(0);
  await expect(
    page.getByRole("option", { name: "Receipt", exact: true }),
  ).toHaveCount(0);
  await denied(page, "records/budgets");
  await denied(page, "workflows/payments", {
    purchaseRequestId: 1,
    invoiceId: 1,
    amount: 1,
    method: "Cash",
    reference: "FORBIDDEN",
    payee: "Supplier",
    date: "2026-11-22",
    overrideReason: "",
    finalPayment: false,
  });
});
test("Department Head remains department-scoped and cannot approve", async ({
  page,
}) => {
  const me = await login(page, "head@missionschool.local");
  await expect(
    page.getByRole("link", { name: "Users", exact: true }),
  ).toHaveCount(0);
  await page.goto("/#requests");
  await expect(
    page.getByRole("button", { name: "+ New request" }),
  ).toBeVisible();
  const rows = await (await page.request.get("/api/records/requests")).json();
  expect(
    rows.every((r: any) => r.departmentId === me.departmentId),
  ).toBeTruthy();
  await expect(page.locator(".table-panel")).toBeVisible();
  await denied(page, "records/payments");
  await denied(page, "workflows/requests/" + rows[0].id + "/status", {
    status: "Approved",
    amount: 1,
    comment: "Forbidden",
    supplierId: 1,
  });
});
test("Staff can request but cannot see financial administration or stock editing", async ({
  page,
}) => {
  await login(page, "kitchen@missionschool.local");
  for (const name of [
    "Users",
    "Payments",
    "Expenses",
    "Stock Movements",
    "Income & Donations",
  ])
    await expect(page.getByRole("link", { name, exact: true })).toHaveCount(0);
  await page.goto("/#requests");
  await expect(
    page.getByRole("button", { name: "+ New request" }),
  ).toBeVisible();
  await page.goto("/#inventory");
  await expect(page.getByRole("button", { name: "+ New record" })).toHaveCount(
    0,
  );
  await denied(page, "records/movements");
  await denied(page, "records/expenses");
  await denied(page, "records/payments");
});
test("Auditor sees all-school records with no write, upload or approval controls", async ({
  page,
}) => {
  const me = await login(page, "auditor@missionschool.local");
  expect(me.access.payments.all).toBe(true);
  await expect(
    page.getByRole("link", { name: "Users", exact: true }),
  ).toHaveCount(0);
  for (const resource of [
    "budgets",
    "expenses",
    "payments",
    "suppliers",
    "income",
    "inventory",
    "assets",
    "petty-accounts",
  ]) {
    await page.goto("/#" + resource);
    await expect(page.getByRole("heading", { level: 1 })).toBeVisible();
    await expect(
      page.getByRole("button", { name: "+ New record" }),
    ).toHaveCount(0);
  }
  await page.goto("/#requests");
  await page
    .locator(".table-panel")
    .getByRole("button", { name: "Open ↗" })
    .first()
    .click();
  await expect(page.getByRole("button", { name: "Save record" })).toHaveCount(
    0,
  );
  await expect(
    page.getByRole("button", { name: "Confirm decision" }),
  ).toHaveCount(0);
  await expect(
    page.getByRole("button", { name: "Attach document" }),
  ).toHaveCount(0);
  await expect(page.getByLabel("Evidence file")).toHaveCount(0);
  await denied(page, "records/expenses");
  await denied(page, "records/inventory");
  await denied(page, "records/audit");
});
test("Owner can assign all six roles and create Finance without a department", async ({
  page,
}) => {
  await login(page, "admin@missionschool.local", "Admin123!");
  await page.goto("/#users");
  await page.getByRole("button", { name: "+ New record" }).click();
  const role = page.getByLabel("Access role");
  for (const name of [
    "ADMIN",
    "FINANCE",
    "STOREKEEPER",
    "DEPARTMENT_HEAD",
    "STAFF",
    "AUDITOR",
  ])
    await expect(
      role.locator("option").filter({ hasText: new RegExp("^" + name + "$") }),
    ).toHaveCount(1);
  await role.selectOption("FINANCE");
  await page.getByLabel("Full name").fill("Browser Finance " + Date.now());
  await page
    .getByLabel("Email / username")
    .fill("browserfinance" + Date.now() + "@missionschool.local");
  await page.getByLabel("Job title").fill("Bursar");
  await page
    .getByLabel("Temporary / reset password")
    .fill("BrowserFinance123!");
  await page.getByRole("button", { name: "Save record", exact: true }).click();
  await expect(page.getByRole("dialog")).toHaveCount(0);
});

test("A revoked role session clears stale UI and can sign in with fresh permissions", async ({
  page,
  browser,
}) => {
  await login(page, "admin@missionschool.local", "Admin123!");
  const departments = await (
    await page.request.get("/api/records/departments")
  ).json();
  const dept = departments.find((d: any) => d.name === "Kitchen").id;
  const body = {
    name: "Session browser test",
    email: "session" + Date.now() + "@missionschool.local",
    role: "STAFF",
    departmentId: dept,
    position: "Worker",
    password: "SessionBrowser123!",
    active: true,
  };
  const token = (await (await page.request.get("/api/auth/csrf")).json()).token;
  const created = await page.request.post("/api/records/users", {
    headers: { "X-CSRF-TOKEN": token },
    data: body,
  });
  expect(created.ok()).toBeTruthy();
  const id = (await created.json()).id;
  const context = await browser.newContext();
  const worker = await context.newPage();
  await login(worker, body.email, body.password);
  await worker.goto("/#requests");
  await worker.getByRole("button", { name: "+ New request" }).click();
  await worker.getByLabel("Budget allocation").selectOption({ index: 1 });
  await worker
    .getByLabel("Purpose", { exact: false })
    .first()
    .fill("Session permission check");
  await worker.getByLabel("Item description", { exact: true }).fill("Pencils");
  await worker.getByLabel("Unit price in KES", { exact: true }).fill("50");
  await worker
    .getByRole("button", { name: "Save record", exact: true })
    .click();
  await expect(worker.getByRole("dialog")).toHaveCount(0);
  const { password: unused, ...editBody } = body;
  const changeToken = (await (await page.request.get("/api/auth/csrf")).json())
    .token;
  expect(
    (
      await page.request.put("/api/records/users/" + id, {
        headers: { "X-CSRF-TOKEN": changeToken },
        data: { ...editBody, role: "AUDITOR", departmentId: null },
      })
    ).ok(),
  ).toBeTruthy();
  await worker.goto("/#budgets");
  await expect(
    worker.getByRole("heading", { name: "Sign in to your school" }),
  ).toBeVisible();
  await worker.getByLabel("Email", { exact: true }).fill(body.email);
  await worker.getByLabel("Password", { exact: true }).fill(body.password);
  await worker.getByRole("button", { name: "Sign in →" }).click();
  await expect(
    worker.getByRole("heading", {
      name: /Every shilling, accounted for\.|Approve purchase requests/,
    }),
  ).toBeVisible();
  await worker.goto("/#requests");
  await expect(
    worker.getByRole("button", { name: "+ New request" }),
  ).toHaveCount(0);
  expect((await (await worker.request.get("/api/auth/me")).json()).role).toBe(
    "AUDITOR",
  );
  await context.close();
});
